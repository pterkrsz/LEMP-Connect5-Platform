using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LEMP.Application.Inverter;
using LEMP.Application.Modbus;
using LEMP.Domain.Inverter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LEMP.Infrastructure.Services;

public class InverterInfluxForwarder : BackgroundService
{
    private readonly IHttpClientFactory _factory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InverterInfluxForwarder> _logger;
    private readonly string _serialPort;
    private readonly byte _slaveId;
    private readonly int _pollingIntervalSeconds;
    private readonly IReadOnlyDictionary<string, List<DeyeModbusRegisterDefinition>> _groups;

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
        var mapFile = inverterSection["MapFile"]
                       ?? throw new InvalidOperationException("Inverter:MapFile is not configured");
        var mapPath = ResolvePath(mapFile);
        _groups = DeyeModbusMapLoader.Load(mapPath);
        if (_groups.Count == 0)
        {
            throw new InvalidOperationException("No active inverter registers were loaded from the map file.");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ModbusRTUReader? reader = null;
        InverterModbusAdapter? adapter = null;

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

        try
        {
            try
            {
                reader = new ModbusRTUReader(_serialPort);
                adapter = new InverterModbusAdapter(reader, _groups, _slaveId);
            }
            catch (Exception initEx)
            {
                reader?.Dispose();
                _logger.LogError(initEx, "Failed to initialize inverter Modbus communication");
                throw;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                if (adapter != null)
                {
                    try
                    {
                        var snapshot = adapter.ReadSnapshot();
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
                                    _logger.LogWarning("Failed to push inverter metrics to InfluxDB: {Status} {Body}", response.StatusCode, body);
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Inverter read failed; attempting to reconnect");
                            adapter = null;
                            try
                            {
                                reader?.Dispose();
                            }
                            catch (Exception disposeEx)
                            {
                                _logger.LogDebug(disposeEx, "Error while disposing inverter serial port");
                            }

                            reader = null;

                            ModbusRTUReader? newReader = null;
                            try
                            {
                                newReader = new ModbusRTUReader(_serialPort);
                                var newAdapter = new InverterModbusAdapter(newReader, _groups, _slaveId);
                                reader = newReader;
                                adapter = newAdapter;
                                _logger.LogInformation("Inverter connection re-established");
                            }
                            catch (Exception reconnectionException)
                            {
                                newReader?.Dispose();
                                _logger.LogError(reconnectionException, "Failed to re-establish inverter connection");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error forwarding inverter data");
                        adapter = null;
                        try
                        {
                            reader?.Dispose();
                        }
                        catch (Exception disposeEx)
                        {
                            _logger.LogDebug(disposeEx, "Error while disposing inverter serial port after failure");
                        }

                        reader = null;

                        ModbusRTUReader? newReader = null;
                        try
                        {
                            newReader = new ModbusRTUReader(_serialPort);
                            var newAdapter = new InverterModbusAdapter(newReader, _groups, _slaveId);
                            reader = newReader;
                            adapter = newAdapter;
                            _logger.LogInformation("Inverter connection re-established");
                        }
                        catch (Exception reconnectionException)
                        {
                            newReader?.Dispose();
                            _logger.LogError(reconnectionException, "Failed to re-establish inverter connection");
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Inverter Modbus adapter is not initialized; attempting to reconnect");

                    ModbusRTUReader? newReader = null;
                    try
                    {
                        newReader = new ModbusRTUReader(_serialPort);
                        var newAdapter = new InverterModbusAdapter(newReader, _groups, _slaveId);
                        reader = newReader;
                        adapter = newAdapter;
                        _logger.LogInformation("Inverter connection re-established");
                    }
                    catch (Exception reconnectionException)
                    {
                        newReader?.Dispose();
                        _logger.LogError(reconnectionException, "Failed to re-establish inverter connection");
                    }
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
        finally
        {
            reader?.Dispose();
        }
    }

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

            var sb = new StringBuilder();
            sb.Append("inverter");
            sb.Append(",node=").Append(EscapeTagValue(node));
            sb.Append(",group=").Append(EscapeTagValue(group.Key));
            sb.Append(' ');

            bool first = true;
            foreach (var field in group.Value.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (double.IsNaN(field.Value) || double.IsInfinity(field.Value))
                {
                    continue;
                }

                if (!first)
                {
                    sb.Append(',');
                }

                sb.Append(EscapeFieldKey(field.Key));
                sb.Append('=');
                sb.Append(field.Value.ToString(inv));
                first = false;
            }

            if (first)
            {
                continue;
            }

            sb.Append(' ').Append(timestamp);
            lines.Add(sb.ToString());
        }

        return lines;
    }

    private static string EscapeTagValue(string value) =>
        value.Replace("\\", "\\\\").Replace(",", "\\,").Replace(" ", "\\ ").Replace("=", "\\=");

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

    private static string ResolvePath(string path) =>
        Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
}
