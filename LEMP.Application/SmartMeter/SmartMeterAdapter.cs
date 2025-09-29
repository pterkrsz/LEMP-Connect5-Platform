using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using LEMP.Application.Modbus;
using LEMP.Domain.SmartMeter;

namespace LEMP.Application.SmartMeter;

public class SmartMeterAdapter
{
    private static readonly IReadOnlyDictionary<string, Action<SmartMeterState, double>> FieldSetters =
        new Dictionary<string, Action<SmartMeterState, double>>(StringComparer.OrdinalIgnoreCase)
        {
            ["voltagelinetoneutral"] = (s, v) => s.VoltageLineToNeutral = v,
            ["current"] = (s, v) => s.Current = v,
            ["activepower"] = (s, v) => s.ActivePower = v,
            ["apparentpower"] = (s, v) => s.ApparentPower = v,
            ["reactivepower"] = (s, v) => s.ReactivePower = v,
            ["powerfactor"] = (s, v) => s.PowerFactor = v,
            ["frequency"] = (s, v) => s.Frequency = v,
            ["importedactiveenergy"] = (s, v) => s.ImportedActiveEnergy = v,
            ["exportedactiveenergy"] = (s, v) => s.ExportedActiveEnergy = v,
            ["importedreactiveenergy"] = (s, v) => s.ImportedReactiveEnergy = v,
            ["exportedreactiveenergy"] = (s, v) => s.ExportedReactiveEnergy = v,
            ["totalactiveenergy"] = (s, v) => s.TotalActiveEnergy = v
        };

    private readonly ModbusRTUReader _reader;
    private readonly IReadOnlyList<SmartMeterRegisterDefinition> _registers;

    public SmartMeterAdapter(ModbusRTUReader reader, string mapPath)
    {
        _reader = reader;
        _registers = LoadRegisterDefinitions(mapPath);
        if (_registers.Count == 0)
        {
            throw new InvalidOperationException("No smart meter registers were loaded from the map file.");
        }
    }

    public SmartMeterState ReadSmartMeterState()
    {
        var state = new SmartMeterState();
        bool allOk = true;

        foreach (var definition in _registers)
        {
            var request = new RegisterReadRequest<float>
            {
                SlaveId = definition.SlaveId,
                StartAddress = definition.StartAddress,
                RegisterCount = definition.RegisterCount,
                FunctionCode = definition.FunctionCode,
                OnValue = raw => definition.Apply(state, raw)
            };

            if (!_reader.TryRead(request))
            {
                allOk = false;
            }
        }

        state.SmartMeterAlive = allOk;
        return state;
    }

    private static IReadOnlyList<SmartMeterRegisterDefinition> LoadRegisterDefinitions(string jsonPath)
    {
        if (!File.Exists(jsonPath))
        {
            throw new FileNotFoundException($"Smart meter mapping file not found: {jsonPath}");
        }

        var json = File.ReadAllText(jsonPath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        var rows = JsonSerializer.Deserialize<List<SmartMeterJsonRow>>(json, options) ?? new List<SmartMeterJsonRow>();
        var definitions = new List<SmartMeterRegisterDefinition>();

        foreach (var row in rows)
        {
            if (!IsTrue(row.Active))
            {
                continue;
            }

            var fieldKey = Normalize(row.Name);
            if (string.IsNullOrEmpty(fieldKey) || !FieldSetters.TryGetValue(fieldKey, out var setter))
            {
                continue;
            }

            if (!IsFloatDataType(row.DataType))
            {
                continue;
            }

            if (!ushort.TryParse(row.ReadAddress, NumberStyles.Integer, CultureInfo.InvariantCulture, out var address))
            {
                continue;
            }

            var registerCount = ParseRegisterCount(row.Length);
            var functionCode = ParseByte(row.ReadFunctionCode, defaultValue: 4);
            var slaveId = ParseByte(row.SlaveId, defaultValue: 1);
            var factor = ParseFactor(row.Factor);

            definitions.Add(new SmartMeterRegisterDefinition(
                slaveId,
                address,
                registerCount,
                functionCode,
                factor,
                setter));
        }

        return definitions;
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

    private static bool IsFloatDataType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true; // Default to float if unspecified.
        }

        var normalized = value.Trim().ToLowerInvariant();
        return normalized is "float" or "float32" or "single";
    }

    private static ushort ParseRegisterCount(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)
            && ushort.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            && parsed > 0)
        {
            return parsed;
        }

        return 2;
    }

    private static byte ParseByte(string? value, byte defaultValue)
    {
        if (!string.IsNullOrWhiteSpace(value)
            && byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return defaultValue;
    }

    private static double ParseFactor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 1.0;
        }

        var trimmed = value.Trim();
        if (trimmed.Contains('/', StringComparison.Ordinal))
        {
            double result = 1.0;
            foreach (var part in trimmed.Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                if (double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                {
                    result *= parsed;
                }
            }

            return result;
        }

        return double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var valueParsed)
            ? valueParsed
            : 1.0;
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToLowerInvariant(ch));
            }
        }

        return sb.ToString();
    }

    private sealed class SmartMeterRegisterDefinition
    {
        private readonly Action<SmartMeterState, double> _setter;

        public SmartMeterRegisterDefinition(
            byte slaveId,
            ushort startAddress,
            ushort registerCount,
            byte functionCode,
            double factor,
            Action<SmartMeterState, double> setter)
        {
            SlaveId = slaveId;
            StartAddress = startAddress;
            RegisterCount = registerCount;
            FunctionCode = functionCode;
            Factor = factor;
            _setter = setter;
        }

        public byte SlaveId { get; }
        public ushort StartAddress { get; }
        public ushort RegisterCount { get; }
        public byte FunctionCode { get; }
        public double Factor { get; }

        public void Apply(SmartMeterState state, float rawValue)
        {
            var scaled = rawValue * Factor;
            _setter(state, scaled);
        }
    }

    private sealed class SmartMeterJsonRow
    {
        public string? Active { get; set; }
        public string? ReadFunctionCode { get; set; }
        public string? ReadAddress { get; set; }
        public string? Length { get; set; }
        public string? Name { get; set; }
        public string? DataType { get; set; }
        public string? Factor { get; set; }
        public string? SlaveId { get; set; }
    }
}
