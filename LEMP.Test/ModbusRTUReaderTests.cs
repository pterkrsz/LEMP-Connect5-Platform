using System;
using System.Linq;
using System.Reflection;
using LEMP.Application.Modbus;
using NUnit.Framework;

namespace LEMP.Test;

public class ModbusRTUReaderTests
{
    private static T InvokePrivate<T>(string methodName, object?[] parameters, Type? genericType = null)
    {
        var method = typeof(ModbusRTUReader).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == methodName);
        Assert.That(method, Is.Not.Null, $"Method {methodName} not found");
        if (method!.IsGenericMethod)
        {
            Assert.That(genericType, Is.Not.Null, "Generic type must be provided for generic methods");
            method = method.MakeGenericMethod(genericType);
        }
        return (T)method.Invoke(null, parameters)!;
    }

    [Test]
    public void CalculateCrc_KnownData_MatchesExpected()
    {
        var data = new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A };
        var crc = InvokePrivate<byte[]>("CalculateCrc", new object?[] { data, data.Length });
        Assert.That(crc, Is.EquivalentTo(new byte[] { 0xC5, 0xCD }));
    }

    [Test]
    public void BuildFrame_CreatesProperFrame()
    {
        var request = new RegisterReadRequest<ushort>
        {
            SlaveId = 1,
            FunctionCode = 3,
            StartAddress = 0,
            RegisterCount = 10
        };
        var frame = InvokePrivate<byte[]>("BuildFrame", new object?[]
        {
            request.SlaveId,
            request.FunctionCode,
            request.StartAddress,
            request.RegisterCount
        });
        Assert.That(frame[0], Is.EqualTo(request.SlaveId));
        Assert.That(frame[1], Is.EqualTo(request.FunctionCode));
        Assert.That(frame[2], Is.EqualTo(0));
        Assert.That(frame[3], Is.EqualTo(0));
        Assert.That(frame[4], Is.EqualTo(0));
        Assert.That(frame[5], Is.EqualTo(10));
        var crc = InvokePrivate<byte[]>("CalculateCrc", new object?[] { frame, 6 });
        Assert.That(frame.Skip(6).Take(2).ToArray(), Is.EqualTo(crc));
    }

    [Test]
    public void ConvertBytes_Float_ReturnsExpected()
    {
        var bytes = BitConverter.GetBytes(123.45f);
        var result = InvokePrivate<float>("ConvertBytes", new object?[] { bytes }, typeof(float));
        Assert.That(result, Is.EqualTo(123.45f));
    }
}
