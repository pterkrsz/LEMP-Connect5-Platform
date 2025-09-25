using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace LEMP.Application.Inverter;

public static class DeyeModbusMapLoader
{
    public static IReadOnlyDictionary<string, List<DeyeModbusRegisterDefinition>> Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Mapping file not found: {filePath}");
        }

        var json = File.ReadAllText(filePath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var entries = JsonSerializer.Deserialize<List<DeyeRegisterEntry>>(json, options) ?? new List<DeyeRegisterEntry>();

        var activeDefinitions = entries
            .Where(e => e.IsActive && e.ParsedReadFunctionCode is 3 or 4)
            .Where(e => e.ParsedReadAddress.HasValue && e.ParsedLength.HasValue && e.ParsedLength.Value > 0)
            .Where(e => !string.IsNullOrWhiteSpace(e.Name) && !string.IsNullOrWhiteSpace(e.Group) && !string.IsNullOrWhiteSpace(e.DateType))
            .Select(e => new DeyeModbusRegisterDefinition(
                e.Group!.Trim(),
                e.Name!.Trim(),
                e.ParsedReadFunctionCode!.Value,
                e.ParsedReadAddress!.Value,
                e.ParsedLength!.Value,
                e.DateType!.Trim(),
                ParseScale(e.Factor),
                string.IsNullOrWhiteSpace(e.Unit) ? null : e.Unit!.Trim()))
            .ToList();

        return activeDefinitions
            .GroupBy(d => d.Group, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Address).ThenBy(x => x.Name).ToList(), StringComparer.OrdinalIgnoreCase);
    }

    private static double ParseScale(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            return 1d;
        }

        var cleaned = raw.Trim();
        if (cleaned.Contains('/'))
        {
            var parts = cleaned.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 2
                && double.TryParse(parts[0], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var numerator)
                && double.TryParse(parts[1], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var denominator)
                && Math.Abs(denominator) > double.Epsilon)
            {
                return numerator / denominator;
            }

            var first = parts.FirstOrDefault();
            if (first != null
                && double.TryParse(first, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var fallback))
            {
                return fallback;
            }
        }

        if (double.TryParse(cleaned, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        return 1d;
    }

    private sealed class DeyeRegisterEntry
    {
        public string? Active { get; set; }

        public string? ReadFunctionCode { get; set; }

        public string? ReadAddress { get; set; }

        public string? Length { get; set; }

        public string? Name { get; set; }

        public string? DateType { get; set; }

        public string? Factor { get; set; }

        public string? Unit { get; set; }

        public string? Group { get; set; }

        public bool IsActive => Active != null && Active.Equals("true", StringComparison.OrdinalIgnoreCase);

        public byte? ParsedReadFunctionCode => TryParseByte(ReadFunctionCode);

        public ushort? ParsedReadAddress => TryParseUShort(ReadAddress);

        public ushort? ParsedLength => TryParseUShort(Length);

        private static byte? TryParseByte(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var trimmed = raw.Trim();
            try
            {
                return trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                    ? Convert.ToByte(trimmed, 16)
                    : Convert.ToByte(trimmed, 10);
            }
            catch
            {
                return null;
            }
        }

        private static ushort? TryParseUShort(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var trimmed = raw.Trim();
            try
            {
                return trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                    ? Convert.ToUInt16(trimmed, 16)
                    : Convert.ToUInt16(trimmed, 10);
            }
            catch
            {
                return null;
            }
        }
    }
}
