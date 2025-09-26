using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LEMP.Domain.Inverter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NModbus;
using NModbus.Interfaces;
using NModbus.Serial;

namespace LEMP.Infrastructure.Services;

public class InverterInfluxForwarder : BackgroundService
{
    private readonly IHttpClientFactory _factory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InverterInfluxForwarder> _logger;
    private readonly string _serialPort;
    private readonly int _baudRate;
    private readonly Parity _parity;
    private readonly int _dataBits;
    private readonly StopBits _stopBits;
    private readonly byte _slaveId;
    private readonly int _pollingIntervalSeconds;
    private readonly int _readTimeoutMs;
    private readonly int _writeTimeoutMs;
    private readonly IReadOnlyList<DeyeRegisterDefinition> _registers;

    private enum RegisterDataType
    {
        UInt16,
        Int16,
        UInt32,
        Int32
    }

    public InverterInfluxForwarder(
        IHttpClientFactory factory,
        IConfiguration configuration,
        ILogger<InverterInfluxForwarder> logger)
    {
        _factory = factory;
        _configuration = configuration;
        _logger = logger;

        var inverterSection = configuration.GetRequiredSection("Inverter");
        _serialPort = inverterSection["SerialPort"]
                      ?? throw new InvalidOperationException("Inverter:SerialPort is not configured");
        _pollingIntervalSeconds = inverterSection.GetValue<int?>("PollingIntervalSeconds")
                                  ?? throw new InvalidOperationException("Inverter:PollingIntervalSeconds is not configured");
        _slaveId = (byte)(inverterSection.GetValue<int?>("SlaveId") ?? 1);
        _baudRate = inverterSection.GetValue("BaudRate", 9600);
        _parity = ParseParity(inverterSection["Parity"]) ?? Parity.None;
        _dataBits = inverterSection.GetValue("DataBits", 8);
        _stopBits = ParseStopBits(inverterSection["StopBits"]) ?? StopBits.One;
        _readTimeoutMs = inverterSection.GetValue("ReadTimeoutMs", 2000);
        _writeTimeoutMs = inverterSection.GetValue("WriteTimeoutMs", 2000);

        var mapFile = inverterSection["MapFile"]
                       ?? throw new InvalidOperationException("Inverter:MapFile is not configured");
        var mapPath = ResolvePath(mapFile);
        _registers = LoadRegisterDefinitions(mapPath);
        if (_registers.Count == 0)
        {
            throw new InvalidOperationException("No active inverter registers were loaded from the map file.");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var client = _factory.CreateClient("Influx");
        var token = _configuration["InfluxDB:Token"];
        var db = _configuration["InfluxDB:Bucket"]
                 ?? throw new InvalidOperationException("InfluxDB:Bucket is not configured");
        var node = _configuration["InfluxDB:NodeId"]
                   ?? throw new InvalidOperationException("InfluxDB:NodeId is not configured");
        var url = $"/api/v3/write_lp?db={Uri.EscapeDataString(db)}&precision=nanosecond&accept_partial=true";

        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", token);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var port = new SerialPort(_serialPort, _baudRate, _parity, _dataBits, _stopBits)
                {
                    ReadTimeout = _readTimeoutMs,
                    WriteTimeout = _writeTimeoutMs
                };
                port.Open();

                var factory = new ModbusFactory();
                using var adapter = new SerialPortAdapter(port);
                using var master = factory.CreateRtuMaster(adapter);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var snapshot = ReadSnapshot(master);
                    if (snapshot.InverterAlive && snapshot.Groups.Count > 0)
                    {
                        var lines = BuildLineProtocol(node, snapshot);
                        if (lines.Count > 0)
                        {
                            var content = new StringContent(string.Join('\n', lines), Encoding.UTF8, "text/plain");
                            var response = await client.PostAsync(url, content, stoppingToken);
                            if (!response.IsSuccessStatusCode)
                            {
                                var body = await response.Content.ReadAsStringAsync(stoppingToken);
                                _logger.LogWarning(
                                    "Failed to push inverter metrics to InfluxDB: {Status} {Body}",
                                    response.StatusCode,
                                    body);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Inverter read failed; data set is empty or incomplete");
                    }

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(_pollingIntervalSeconds), stoppingToken);
                    }
                    catch (TaskCanceledException)
                    {
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Inverter polling cycle failed");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_pollingIntervalSeconds), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                }
            }
        }
    }

    private InverterSnapshot ReadSnapshot(IModbusMaster master)
    {
        var snapshot = new InverterSnapshot
        {
            InverterAlive = true,
            Timestamp = DateTimeOffset.UtcNow
        };

        var valuesByGroup = new Dictionary<string, Dictionary<string, InverterRegisterValue>>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in _registers)
        {
            try
            {
                var (scaled, raw) = ReadRegister(master, _slaveId, definition);
                if (!valuesByGroup.TryGetValue(definition.Group, out var groupValues))
                {
                    groupValues = new Dictionary<string, InverterRegisterValue>(StringComparer.OrdinalIgnoreCase);
                    valuesByGroup[definition.Group] = groupValues;
                }

                groupValues[definition.Name] = new InverterRegisterValue(
                    scaled,
                    raw,
                    definition.DataType,
                    definition.Factor,
                    definition.Unit);
            }
            catch (Exception ex)
            {
                snapshot.InverterAlive = false;
                _logger.LogDebug(
                    ex,
                    "Failed to read inverter register {Address} ({Name})",
                    definition.Address,
                    definition.Name);
            }
        }

        foreach (var kvp in valuesByGroup)
        {
            if (kvp.Value.Count > 0)
            {
                snapshot.Groups[kvp.Key] = kvp.Value;
            }
        }

        if (snapshot.Groups.Count == 0)
        {
            snapshot.InverterAlive = false;
        }

        return snapshot;
    }

    private static (double Scaled, double Raw) ReadRegister(IModbusMaster master, byte slaveId, DeyeRegisterDefinition definition)
    {
        var registers = master.ReadHoldingRegisters(slaveId, definition.Address, (ushort)definition.WordLength);

        if (!definition.TryConvert(registers, out var rawValue))
        {
            throw new InvalidOperationException(
                $"Unsupported register layout for {definition.Name} ({definition.DataType}) at address {definition.Address}");
        }

        var scaled = rawValue * definition.Factor;
        return (scaled, rawValue);
    }

    private static IReadOnlyList<DeyeRegisterDefinition> LoadRegisterDefinitions(string jsonPath)
    {
        if (!File.Exists(jsonPath))
        {
            throw new FileNotFoundException($"Mapping file not found: {jsonPath}");
        }

        var json = File.ReadAllText(jsonPath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        var rows = JsonSerializer.Deserialize<List<DeyeJsonRow>>(json, options) ?? new List<DeyeJsonRow>();
        var regs = new List<DeyeRegisterDefinition>();

        foreach (var row in rows)
        {
            if (!IsTrue(row.Active))
            {
                continue;
            }

            if (!string.Equals(row.ReadFunctionCode?.Trim(), "3", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!ushort.TryParse(row.ReadAddress, NumberStyles.Integer, CultureInfo.InvariantCulture, out var address))
            {
                continue;
            }

            if (!TryParseDataType(row.DataType, out var registerDataType, out var normalizedType))
            {
                continue;
            }

            var requiredWordLength = GetRequiredWordLength(registerDataType);
            var wordLength = requiredWordLength;
            if (!string.IsNullOrWhiteSpace(row.Length)
                && int.TryParse(row.Length, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedLength)
                && parsedLength >= requiredWordLength)
            {
                wordLength = parsedLength;
            }

            var factor = ParseFactor(row.Factor);
            var group = row.Group?.Trim();
            var name = row.Name?.Trim();

            if (string.IsNullOrEmpty(group) || string.IsNullOrEmpty(name))
            {
                continue;
            }

            regs.Add(new DeyeRegisterDefinition(
                address,
                wordLength,
                name,
                normalizedType,
                registerDataType,
                factor,
                group,
                row.Unit?.Trim()));
        }

        return regs
            .OrderBy(r => r.Group, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.Address)
            .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsTrue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim().ToLowerInvariant();
        return normalized is "true" or "1" or "yes" or "y";
    }

    private static double ParseFactor(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || string.Equals(raw.Trim(), "null", StringComparison.OrdinalIgnoreCase))
        {
            return 1.0;
        }

        var text = raw.Trim();
        if (text.Contains('/'))
        {
            var parts = text.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length > 1 &&
                double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var second))
            {
                return second;
            }

            if (parts.Length > 0 &&
                double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var first))
            {
                return first;
            }
        }

        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : 1.0;
    }

    private static Parity? ParseParity(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return Enum.TryParse<Parity>(raw, true, out var parity) ? parity : null;
    }

    private static StopBits? ParseStopBits(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return Enum.TryParse<StopBits>(raw, true, out var stopBits) ? stopBits : null;
    }

    private static bool TryParseDataType(string? raw, out RegisterDataType dataType, out string normalized)
    {
        normalized = (raw ?? "uint16").Trim();
        if (normalized.Length == 0)
        {
            normalized = "uint16";
        }

        switch (normalized.ToLowerInvariant())
        {
            case "uint16":
                dataType = RegisterDataType.UInt16;
                normalized = "uint16";
                return true;
            case "int16":
                dataType = RegisterDataType.Int16;
                normalized = "int16";
                return true;
            case "uint32":
                dataType = RegisterDataType.UInt32;
                normalized = "uint32";
                return true;
            case "int32":
                dataType = RegisterDataType.Int32;
                normalized = "int32";
                return true;
            default:
                dataType = default;
                return false;
        }
    }

    private static int GetRequiredWordLength(RegisterDataType type) =>
        type switch
        {
            RegisterDataType.UInt16 => 1,
            RegisterDataType.Int16 => 1,
            RegisterDataType.UInt32 => 2,
            RegisterDataType.Int32 => 2,
            _ => 1
        };

    private static List<string> BuildLineProtocol(string node, InverterSnapshot snapshot)
    {
        var lines = new List<string>();
        var timestamp = snapshot.Timestamp.ToUnixTimeMilliseconds() * 1_000_000;
        var inv = CultureInfo.InvariantCulture;

        foreach (var group in snapshot.Groups)
        {
            if (group.Value.Count == 0)
            {
                continue;
            }

            foreach (var register in group.Value.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
            {
                var registerValue = register.Value;
                var sb = new StringBuilder();
                sb.Append("inverter");
                sb.Append(",node=").Append(EscapeTagValue(node));
                sb.Append(",group=").Append(EscapeTagValue(group.Key));
                sb.Append(",register=").Append(EscapeTagValue(register.Key));
                AppendOptionalTag(sb, "data_type", registerValue.DataType);
                AppendOptionalTag(sb, "unit", registerValue.Unit);

                sb.Append(' ');
                var first = true;
                if (!TryAppendField(sb, ref first, "value", registerValue.Value, inv))
                {
                    continue;
                }

                if (!double.IsNaN(registerValue.RawValue) && !double.IsInfinity(registerValue.RawValue))
                {
                    TryAppendField(sb, ref first, "raw_value", registerValue.RawValue, inv);
                }

                if (!double.IsNaN(registerValue.Scale) && !double.IsInfinity(registerValue.Scale))
                {
                    TryAppendField(sb, ref first, "scale", registerValue.Scale, inv);
                }

                sb.Append(' ').Append(timestamp);
                lines.Add(sb.ToString());
            }
        }

        return lines;
    }

    private static string EscapeTagValue(string value) =>
        value.Replace("\\", "\\\\").Replace(",", "\\,").Replace(" ", "\\ ").Replace("=", "\\=");

    private static void AppendOptionalTag(StringBuilder sb, string tagName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (string.Equals(value, "null", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        sb.Append(',').Append(tagName).Append('=').Append(EscapeTagValue(value));
    }

    private static string EscapeFieldKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return "value";
        }

        var sb = new StringBuilder(key.Length);
        foreach (var ch in key)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToLowerInvariant(ch));
            }
            else
            {
                sb.Append('_');
            }
        }

        var result = sb.ToString().Trim('_');
        return string.IsNullOrEmpty(result) ? "value" : result;
    }

    private static bool TryAppendField(StringBuilder sb, ref bool first, string fieldName, double value, CultureInfo inv)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return false;
        }

        if (!first)
        {
            sb.Append(',');
        }

        sb.Append(EscapeFieldKey(fieldName));
        sb.Append('=');
        sb.Append(value.ToString(inv));
        first = false;
        return true;
    }

    private static string ResolvePath(string path) =>
        Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);

    private sealed class DeyeJsonRow
    {
        public string? Active { get; set; }
        public string? ReadFunctionCode { get; set; }
        public string? ReadAddress { get; set; }
        public string? Length { get; set; }
        public string? Name { get; set; }
        public string? DataType { get; set; }
        public string? Factor { get; set; }
        public string? Unit { get; set; }
        public string? Group { get; set; }
    }

    private sealed class DeyeRegisterDefinition
    {
        public DeyeRegisterDefinition(
            ushort address,
            int wordLength,
            string name,
            string dataType,
            RegisterDataType registerDataType,
            double factor,
            string group,
            string? unit)
        {
            Address = address;
            WordLength = wordLength;
            Name = name;
            DataType = dataType;
            RegisterDataType = registerDataType;
            Factor = factor;
            Group = group;
            Unit = unit;
        }

        public ushort Address { get; }
        public int WordLength { get; }
        public string Name { get; }
        public string DataType { get; }
        public RegisterDataType RegisterDataType { get; }
        public double Factor { get; }
        public string Group { get; }
        public string? Unit { get; }

        public bool TryConvert(ReadOnlySpan<ushort> registers, out double rawValue)
        {
            rawValue = double.NaN;

            if (registers.Length < WordLength)
            {
                return false;
            }

            switch (RegisterDataType)
            {
                case RegisterDataType.UInt16 when WordLength == 1:
                    rawValue = registers[0];
                    return true;
                case RegisterDataType.Int16 when WordLength == 1:
                    rawValue = unchecked((short)registers[0]);
                    return true;
                case RegisterDataType.UInt32 when WordLength >= 2:
                {
                    var combined = ((uint)registers[0] << 16) | registers[1];
                    rawValue = combined;
                    return true;
                }
                case RegisterDataType.Int32 when WordLength >= 2:
                {
                    var combined = ((uint)registers[0] << 16) | registers[1];
                    rawValue = unchecked((int)combined);
                    return true;
                }
                default:
                    return false;
            }
        }
    }
}
