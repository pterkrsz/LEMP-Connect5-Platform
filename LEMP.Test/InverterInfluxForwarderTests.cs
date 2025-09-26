using System;
using System.Collections.Generic;
using System.Reflection;
using LEMP.Domain.Inverter;
using LEMP.Infrastructure.Services;
using NUnit.Framework;

namespace LEMP.Test;

public class InverterInfluxForwarderTests
{
    [Test]
    public void BuildLineProtocol_WritesRowsPerRegister()
    {
        var snapshot = new InverterSnapshot
        {
            InverterAlive = true,
            Timestamp = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero)
        };

        snapshot.Groups["ac"] = new Dictionary<string, InverterRegisterValue>(StringComparer.OrdinalIgnoreCase)
        {
            ["current"] = new InverterRegisterValue(12.34, 123.4, "float", 0.1, "A"),
            ["voltage"] = new InverterRegisterValue(230.5, 2305, "float", 0.1, "V")
        };

        var method = typeof(InverterInfluxForwarder).GetMethod(
            "BuildLineProtocol",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, "Nem sikerült elérni a BuildLineProtocol metódust reflektív módon.");

        var lines = (List<string>?)method!.Invoke(null, new object[] { "node-1", snapshot });

        Assert.That(lines, Is.Not.Null);
        Assert.That(lines!, Has.Count.EqualTo(2));

        var timestamp = snapshot.Timestamp.ToUnixTimeMilliseconds() * 1_000_000;

        var expectedCurrent =
            $"inverter,node=node-1,group=ac,register=current,data_type=float,unit=A value=12.34,raw_value=123.4,scale=0.1 {timestamp}";
        var expectedVoltage =
            $"inverter,node=node-1,group=ac,register=voltage,data_type=float,unit=V value=230.5,raw_value=2305,scale=0.1 {timestamp}";

        Assert.That(lines!, Does.Contain(expectedCurrent));
        Assert.That(lines!, Does.Contain(expectedVoltage));
    }
}
