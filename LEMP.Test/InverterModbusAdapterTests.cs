using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using LEMP.Application.Inverter;
using LEMP.Application.Modbus;
using LEMP.Domain.Inverter;
using NUnit.Framework;

namespace LEMP.Test;

public class InverterModbusAdapterTests
{
    [Test]
    public void DeyeMap_AllActiveRegistersCanBeRead()
    {
        var basePath = TestContext.CurrentContext.TestDirectory;
        var mappingPath = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", "..", "LEMP.Infrastructure", "Config", "deyemodbus.json"));
        Assert.That(File.Exists(mappingPath), Is.True, $"Nem található a Deye Modbus térkép: {mappingPath}");

        var groups = DeyeModbusMapLoader.Load(mappingPath);
        Assert.That(groups, Is.Not.Empty, "Az inverter regisztertérkép üres.");

        using var reader = new FakeModbusRTUReader(groups);
        var adapter = new InverterModbusAdapter(reader, groups, 1);

        var snapshot = adapter.ReadSnapshot();

        Assert.That(snapshot.InverterAlive, Is.True, "Minden Modbus lekérdezésnek sikeresnek kell lennie.");
        Assert.That(snapshot.Groups, Is.Not.Empty, "A lekérdezett csoportok listája nem lehet üres.");

        var definitionsByGroup = groups.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value
                .Where(def => !string.IsNullOrWhiteSpace(def.Name))
                .GroupBy(def => def.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);

        var expectedNamesByGroup = groups.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value
                .Select(def => def.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in expectedNamesByGroup)
        {
            Assert.That(snapshot.Groups.ContainsKey(kvp.Key), Is.True, $"Hiányzik a(z) {kvp.Key} csoport a pillanatképből.");
            var actualNames = snapshot.Groups[kvp.Key].Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var expectedName in kvp.Value)
            {
                Assert.That(actualNames.Contains(expectedName), Is.True,
                    $"A(z) {kvp.Key} csoportból hiányzik a(z) {expectedName} regiszter.");
            }
        }

        var totalExpected = expectedNamesByGroup.Values.Sum(v => v.Count);
        var totalActual = snapshot.Groups.Values.Sum(v => v.Count);
        Assert.That(totalActual, Is.EqualTo(totalExpected), "Minden aktív regiszterértéket vissza kell adni.");

        TestContext.WriteLine("Olvasott inverter Modbus értékek:");

        foreach (var group in snapshot.Groups
                     .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            TestContext.WriteLine($"  Csoport: {group.Key}");

            foreach (var register in group.Value
                         .OrderBy(r => r.Key, StringComparer.OrdinalIgnoreCase))
            {

                if (definitionsByGroup.TryGetValue(group.Key, out var definitions) &&
                    definitions.TryGetValue(register.Key, out var definition))
                {
                    var scaledValue = FormatScaledValue(register.Value, definition);

                    var unit = definition.Unit;
                    var prefix = string.IsNullOrWhiteSpace(unit) || unit.Equals("null", StringComparison.OrdinalIgnoreCase)
                        ? string.Empty
                        : $" {unit}";
                    var scaleSuffix = Math.Abs(definition.Scale - 1d) > double.Epsilon
                        ? $" (scale: {definition.Scale.ToString("G15", CultureInfo.InvariantCulture)})"
                        : string.Empty;

                    var rawValue = FormatRawValue(register.Value, definition);
                    var typeSuffix = string.IsNullOrWhiteSpace(definition.DataType)
                        ? string.Empty
                        : $", type: {definition.DataType}";
                    var lengthSuffix = definition.Length > 0
                        ? $", words: {definition.Length}"
                        : string.Empty;

                    TestContext.WriteLine($"    {register.Key}: {scaledValue}{prefix}{scaleSuffix} (raw: {rawValue}{typeSuffix}{lengthSuffix})");

                }
                else
                {
                    TestContext.WriteLine($"    {register.Key}: {register.Value.Value.ToString(CultureInfo.InvariantCulture)}");
                }

            }
        }

        var planField = typeof(InverterModbusAdapter).GetField("_plan", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(planField, Is.Not.Null, "Nem sikerült elérni az olvasási tervet reflexióval.");

        var plan = planField!.GetValue(adapter) as System.Collections.IEnumerable;
        Assert.That(plan, Is.Not.Null, "Az olvasási terv üres.");

        var expectedRequestCount = plan!.Cast<object>().Count();
        Assert.That(reader.Requests.Count, Is.EqualTo(expectedRequestCount),
            "Minden olvasási batch-hez pontosan egy Modbus lekérdezésnek kell készülnie.");
    }

    private sealed class FakeModbusRTUReader : ModbusRTUReader
    {
        private readonly Dictionary<(byte FunctionCode, ushort Address), ushort> _registerWords;

        public FakeModbusRTUReader(IReadOnlyDictionary<string, List<DeyeModbusRegisterDefinition>> groups)
            : base(new SerialPort(), openPort: false)
        {
            _registerWords = BuildRegisterWordMap(groups);
        }

        public List<(byte FunctionCode, ushort StartAddress, ushort RegisterCount)> Requests { get; } = new();

        public override bool TryReadRegisters(byte slaveId, byte functionCode, ushort startAddress, ushort registerCount,
            out byte[] data)
        {
            Assert.That(functionCode, Is.AnyOf(3, 4), "Csak olvasási funkciókódokat várunk a tesztben.");
            Assert.That(registerCount, Is.GreaterThan(0), "A regiszterszámnak pozitívnak kell lennie.");

            var length = registerCount * 2;
            data = new byte[length];
            var span = data.AsSpan();
            for (var i = 0; i < registerCount; i++)
            {
                var address = (ushort)(startAddress + i);
                var key = (functionCode, address);
                if (!_registerWords.TryGetValue(key, out var word))
                {
                    word = (ushort)(address & 0xFF);
                }

                BinaryPrimitives.WriteUInt16BigEndian(span.Slice(i * 2, 2), word);
            }

            Requests.Add((functionCode, startAddress, registerCount));
            return true;
        }

        private static Dictionary<(byte FunctionCode, ushort Address), ushort> BuildRegisterWordMap(
            IReadOnlyDictionary<string, List<DeyeModbusRegisterDefinition>> groups)
        {
            var map = new Dictionary<(byte FunctionCode, ushort Address), ushort>();

            foreach (var group in groups.Values)
            {
                foreach (var definition in group)
                {
                    if (definition.Length <= 0 || definition.ByteLength <= 0)
                    {
                        continue;
                    }

                    var rawBytes = GenerateRawBytes(definition);
                    var rawSpan = rawBytes.AsSpan();

                    for (var offset = 0; offset < definition.Length; offset++)
                    {
                        var registerAddress = (ushort)(definition.Address + offset);
                        var slice = rawSpan.Slice(offset * 2, 2);
                        var word = BinaryPrimitives.ReadUInt16BigEndian(slice);
                        var key = (definition.FunctionCode, registerAddress);

                        if (!map.ContainsKey(key))
                        {
                            map[key] = word;
                        }
                    }
                }
            }

            return map;
        }

        private static byte[] GenerateRawBytes(DeyeModbusRegisterDefinition definition)
        {
            var bytes = new byte[definition.ByteLength];
            if (bytes.Length == 0)
            {
                return bytes;
            }

            var normalizedType = definition.DataType?.Trim().ToLowerInvariant();
            var scale = Math.Abs(definition.Scale) > double.Epsilon ? definition.Scale : 1d;
            var baseStep = (definition.Address % 10) + 1;

            double magnitude = scale < 1d ? 10d : scale;
            if (definition.ByteLength >= 4 && scale <= 1d)
            {
                magnitude *= 100d;
            }

            var scaledValue = magnitude * baseStep;

            if (normalizedType is "int16" or "int32" && (definition.Address & 1) == 1)
            {
                scaledValue = -scaledValue / 2d;
            }

            var rawValue = scaledValue / scale;

            try
            {
                switch (normalizedType)
                {
                    case "uint16":
                    {
                        var rounded = Math.Round(rawValue, MidpointRounding.AwayFromZero);
                        var clamped = Math.Min(Math.Max(rounded, ushort.MinValue), ushort.MaxValue);
                        var rawU16 = (ushort)clamped;
                        BinaryPrimitives.WriteUInt16BigEndian(bytes, rawU16);
                        break;
                    }
                    case "int16":
                    {
                        var rounded = Math.Round(rawValue, MidpointRounding.AwayFromZero);
                        var clamped = Math.Min(Math.Max(rounded, short.MinValue), short.MaxValue);
                        var rawS16 = (short)clamped;
                        BinaryPrimitives.WriteInt16BigEndian(bytes, rawS16);
                        break;
                    }
                    case "uint32":
                    {
                        var rounded = Math.Round(rawValue, MidpointRounding.AwayFromZero);
                        var clamped = Math.Min(Math.Max(rounded, uint.MinValue), uint.MaxValue);
                        var rawU32 = (uint)clamped;
                        BinaryPrimitives.WriteUInt32BigEndian(bytes, rawU32);
                        break;
                    }
                    case "int32":
                    {
                        var rounded = Math.Round(rawValue, MidpointRounding.AwayFromZero);
                        var clamped = Math.Min(Math.Max(rounded, int.MinValue), int.MaxValue);
                        var rawS32 = (int)clamped;
                        BinaryPrimitives.WriteInt32BigEndian(bytes, rawS32);
                        break;
                    }
                    default:
                        Array.Clear(bytes);
                        break;
                }
            }
            catch
            {
                Array.Clear(bytes);
            }

            return bytes;
        }
    }

    private static string FormatScaledValue(InverterRegisterValue register, DeyeModbusRegisterDefinition definition)
    {
        var normalizedType = definition.DataType?.Trim().ToLowerInvariant();

        return normalizedType switch
        {
            "int16" or "int32" => Math.Round(register.Value, MidpointRounding.AwayFromZero)
                .ToString(CultureInfo.InvariantCulture),
            _ => register.Value.ToString("G15", CultureInfo.InvariantCulture)
        };
    }

    private static string FormatRawValue(InverterRegisterValue register, DeyeModbusRegisterDefinition definition)
    {
        var baseValue = register.RawValue;
        var normalizedType = definition.DataType?.Trim().ToLowerInvariant();

        var valueString = normalizedType switch

        {
            "int16" => ((short)Math.Round(baseValue, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture),
            "uint16" => ((ushort)Math.Round(baseValue, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture),
            "int32" => ((int)Math.Round(baseValue, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture),
            "uint32" => ((uint)Math.Round(baseValue, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture),
            _ => baseValue.ToString("G15", CultureInfo.InvariantCulture)
        };


        if (definition.ByteLength <= 0)
        {
            return valueString;
        }

        Span<byte> buffer = definition.ByteLength <= 32 ? stackalloc byte[definition.ByteLength] : new byte[definition.ByteLength];
        buffer.Clear();

        if (!TryEncodeRawBytes(normalizedType, baseValue, buffer))
        {
            return valueString;
        }

        var hexBytes = string.Join(" ", buffer.ToArray().Select(b => $"0x{b:X2}"));
        return $"{valueString} [{hexBytes}]";
    }

    private static bool TryEncodeRawBytes(string? normalizedType, double baseValue, Span<byte> destination)
    {
        try
        {
            switch (normalizedType)
            {
                case "uint16":
                    if (destination.Length < 2)
                    {
                        return false;
                    }

                    BinaryPrimitives.WriteUInt16BigEndian(destination, checked((ushort)Math.Round(baseValue, MidpointRounding.AwayFromZero)));
                    return true;
                case "int16":
                    if (destination.Length < 2)
                    {
                        return false;
                    }

                    BinaryPrimitives.WriteInt16BigEndian(destination, checked((short)Math.Round(baseValue, MidpointRounding.AwayFromZero)));
                    return true;
                case "uint32":
                    if (destination.Length < 4)
                    {
                        return false;
                    }

                    BinaryPrimitives.WriteUInt32BigEndian(destination, checked((uint)Math.Round(baseValue, MidpointRounding.AwayFromZero)));
                    return true;
                case "int32":
                    if (destination.Length < 4)
                    {
                        return false;
                    }

                    BinaryPrimitives.WriteInt32BigEndian(destination, checked((int)Math.Round(baseValue, MidpointRounding.AwayFromZero)));
                    return true;
                default:
                    return false;
            }
        }
        catch
        {
            return false;
        }

    }
}
