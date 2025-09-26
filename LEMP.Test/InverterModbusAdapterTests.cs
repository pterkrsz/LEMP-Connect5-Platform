using System;
using System.Collections.Generic;
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
                TestContext.WriteLine($"    {register.Key}: {register.Value}");
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
}
