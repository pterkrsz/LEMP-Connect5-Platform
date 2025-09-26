using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using LEMP.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace LEMP.Test;

public class InverterInfluxForwarderTests
{
    [Test]
    public void Constructor_LogsParsedDataTypeAndMultiWordRegister()
    {
        var logger = new TestLogger<InverterInfluxForwarder>();
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(factory => factory.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient());

        var settings = new Dictionary<string, string?>
        {
            ["Inverter:SerialPort"] = "COM1",
            ["Inverter:PollingIntervalSeconds"] = "1",
            ["Inverter:SlaveId"] = "1",
            ["Inverter:MapFile"] = GetMapPath()
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var forwarder = new InverterInfluxForwarder(httpClientFactory.Object, configuration, logger);

        Assert.That(
            logger.Entries.Any(e => e.Message.Contains("Parsed inverter register data type", StringComparison.Ordinal)),
            Is.True,
            "Expected a log entry confirming the data type binding.");

        Assert.That(
            logger.Entries.Any(e => e.Message.Contains("spans", StringComparison.Ordinal)),
            Is.True,
            "Expected a log entry confirming a multi-word register.");

        var registers = GetRegisters(forwarder).ToList();

        Assert.That(registers, Is.Not.Empty);
        Assert.That(registers.Any(r => GetProperty<int>(r, "WordLength") > 1), Is.True);
        Assert.That(registers.All(r => !string.IsNullOrWhiteSpace(GetProperty<string?>(r, "DataType"))), Is.True);
    }

    private static IEnumerable<object> GetRegisters(InverterInfluxForwarder forwarder)
    {
        var field = typeof(InverterInfluxForwarder)
            .GetField("_registers", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null);
        var value = field!.GetValue(forwarder) as System.Collections.IEnumerable;
        Assert.That(value, Is.Not.Null);

        foreach (var item in value!)
        {
            yield return item!;
        }
    }

    private static T GetProperty<T>(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        Assert.That(property, Is.Not.Null);
        return (T)property!.GetValue(instance)!;
    }

    private static string GetMapPath()
    {
        var basePath = AppContext.BaseDirectory;
        var relative = Path.Combine("..", "..", "..", "..", "LEMP.Infrastructure", "Config", "deyemodbus.json");
        var fullPath = Path.GetFullPath(Path.Combine(basePath, relative));
        Assert.That(File.Exists(fullPath), Is.True, $"Map file not found at {fullPath}");
        return fullPath;
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
