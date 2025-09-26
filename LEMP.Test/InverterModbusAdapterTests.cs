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

        using var reader = new FakeModbusRTUReader();
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
                    TestContext.WriteLine($"    {register.Key}: {register.Value.ToString(CultureInfo.InvariantCulture)}");
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
        public FakeModbusRTUReader() : base(new SerialPort(), openPort: false)
        {
        }

        public List<(byte FunctionCode, ushort StartAddress, ushort RegisterCount)> Requests { get; } = new();

        public override bool TryReadRegisters(byte slaveId, byte functionCode, ushort startAddress, ushort registerCount,
            out byte[] data)
        {
            Assert.That(functionCode, Is.AnyOf(3, 4), "Csak olvasási funkciókódokat várunk a tesztben.");
            Assert.That(registerCount, Is.GreaterThan(0), "A regiszterszámnak pozitívnak kell lennie.");

            var length = registerCount * 2;
            data = new byte[length];
            for (var i = 0; i < length; i++)
            {
                data[i] = (byte)((startAddress * 2 + i) & 0xFF);
            }

            Requests.Add((functionCode, startAddress, registerCount));
            return true;
        }
    }

    private static string FormatScaledValue(double value, DeyeModbusRegisterDefinition definition)
    {
        var normalizedType = definition.DataType?.Trim().ToLowerInvariant();

        return normalizedType switch
        {
            "bool" or "boolean" => Math.Abs(value) > 0.5 ? "true" : "false",
            "single" or "float" or "float32" => ((float)value).ToString("G9", CultureInfo.InvariantCulture),
            "double" or "float64" => value.ToString("G17", CultureInfo.InvariantCulture),
            _ => value.ToString("G15", CultureInfo.InvariantCulture)
        };
    }

    private static string FormatRawValue(double scaledValue, DeyeModbusRegisterDefinition definition)
    {
        var scale = Math.Abs(definition.Scale) > double.Epsilon ? definition.Scale : 1d;
        var baseValue = scaledValue / scale;
        var normalizedType = definition.DataType?.Trim().ToLowerInvariant();

        var valueString = normalizedType switch

        {
            "int16" => ((short)Math.Round(baseValue, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture),
            "uint16" => ((ushort)Math.Round(baseValue, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture),
            "int32" => ((int)Math.Round(baseValue, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture),
            "uint32" => ((uint)Math.Round(baseValue, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture),
            "single" or "float" or "float32" => ((float)baseValue).ToString("G9", CultureInfo.InvariantCulture),
            "double" or "float64" => baseValue.ToString("G17", CultureInfo.InvariantCulture),
            "bool" or "boolean" => Math.Abs(baseValue) > 0.5 ? "true" : "false",
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
                case "single" or "float" or "float32":
                    if (destination.Length < 4)
                    {
                        return false;
                    }

                    var floatBits = BitConverter.SingleToInt32Bits((float)baseValue);
                    BinaryPrimitives.WriteInt32BigEndian(destination, floatBits);
                    return true;
                case "double" or "float64":
                    if (destination.Length < 8)
                    {
                        return false;
                    }

                    var doubleBits = BitConverter.DoubleToInt64Bits(baseValue);
                    BinaryPrimitives.WriteInt64BigEndian(destination, doubleBits);
                    return true;
                case "bool" or "boolean":
                    destination[destination.Length - 1] = Math.Abs(baseValue) > 0.5 ? (byte)1 : (byte)0;
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
