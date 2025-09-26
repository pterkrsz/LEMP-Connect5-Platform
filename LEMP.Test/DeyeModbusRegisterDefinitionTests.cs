using System;
using System.Buffers.Binary;
using LEMP.Application.Inverter;
using NUnit.Framework;

namespace LEMP.Test;

[TestFixture]
public class DeyeModbusRegisterDefinitionTests
{
    [Test]
    public void TryConvert_ReadsUnsignedInt16WithScale()
    {
        var definition = CreateDefinition("uint16", length: 1, scale: 0.1);
        Span<byte> raw = stackalloc byte[] { 0x01, 0x2C }; // 300 * 0.1 = 30.0

        var success = definition.TryConvert(raw, out var value);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(value, Is.EqualTo(30.0d));
        });
    }

    [Test]
    public void TryConvert_ReadsSignedInt16()
    {
        var definition = CreateDefinition("int16", length: 1, scale: 1);
        Span<byte> raw = stackalloc byte[] { 0xFF, 0x85 }; // -123

        var success = definition.TryConvert(raw, out var value);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(value, Is.EqualTo(-123d));
        });
    }

    [Test]
    public void TryConvert_ReadsUnsignedInt32()
    {
        var definition = CreateDefinition("uint32", length: 2, scale: 0.1);
        Span<byte> raw = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(raw, 100_000u); // 100_000 * 0.1 = 10_000

        var success = definition.TryConvert(raw, out var value);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(value, Is.EqualTo(10_000d));
        });
    }

    [Test]
    public void TryConvert_ReadsSignedInt32()
    {
        var definition = CreateDefinition("int32", length: 2, scale: 1);
        Span<byte> raw = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(raw, -123_456);

        var success = definition.TryConvert(raw, out var value);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(value, Is.EqualTo(-123_456d));
        });
    }

    [Test]
    public void TryConvert_ReadsFloat()
    {
        var definition = CreateDefinition("float", length: 2, scale: 1);
        Span<byte> raw = stackalloc byte[4];
        var bits = BitConverter.SingleToInt32Bits(123.456f);
        BinaryPrimitives.WriteInt32BigEndian(raw, bits);

        var success = definition.TryConvert(raw, out var value);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(value, Is.EqualTo(123.456f).Within(1e-6));
        });
    }

    [Test]
    public void TryConvert_ReadsBool()
    {
        var definition = CreateDefinition("bool", length: 1, scale: 1);
        Span<byte> raw = stackalloc byte[] { 0x00, 0x01 };

        var success = definition.TryConvert(raw, out var value);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(value, Is.EqualTo(1d));
        });
    }

    [Test]
    public void TryConvert_ReturnsFalseWhenDataTooShort()
    {
        var definition = CreateDefinition("uint32", length: 2, scale: 1);
        Span<byte> raw = stackalloc byte[] { 0x00, 0x01, 0x02 }; // Only 3 bytes provided

        var success = definition.TryConvert(raw, out var value);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(value, Is.EqualTo(0d));
        });
    }

    private static DeyeModbusRegisterDefinition CreateDefinition(string dataType, ushort length, double scale) =>
        new("TestGroup", "TestRegister", 3, 0, length, dataType, scale, unit: null);
}

