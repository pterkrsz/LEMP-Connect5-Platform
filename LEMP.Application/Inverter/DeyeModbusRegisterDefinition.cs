using System;
using System.Buffers.Binary;

namespace LEMP.Application.Inverter;

public sealed class DeyeModbusRegisterDefinition
{
    public DeyeModbusRegisterDefinition(
        string group,
        string name,
        byte functionCode,
        ushort address,
        ushort length,
        string dataType,
        double scale,
        string? unit)
    {
        Group = group;
        Name = name;
        FunctionCode = functionCode;
        Address = address;
        Length = length;
        DataType = dataType;
        Scale = scale;
        Unit = unit;
        _dataType = ParseDataType(dataType);
        if (_dataType == DeyeModbusDataType.Unknown)
        {
            throw new ArgumentException($"Unsupported inverter data type: {dataType}", nameof(dataType));
        }
    }

    private readonly DeyeModbusDataType _dataType;

    public string Group { get; }

    public string Name { get; }

    public byte FunctionCode { get; }

    public ushort Address { get; }

    public ushort Length { get; }

    public string DataType { get; }

    public double Scale { get; }

    public string? Unit { get; }

    public int ByteLength => Length * 2;

    public bool TryConvert(ReadOnlySpan<byte> rawData, out double value)
    {
        value = 0d;

        if (Length == 0 || rawData.Length < ByteLength)
        {
            return false;
        }

        var slice = rawData[..ByteLength];

        if (!TryReadRawValue(slice, out var rawValue))
        {
            return false;
        }

        value = rawValue * Scale;
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    private bool TryReadRawValue(ReadOnlySpan<byte> data, out double rawValue)
    {
        rawValue = 0d;

        return _dataType switch
        {
            DeyeModbusDataType.UInt16 => TryReadUInt16(data, out rawValue),
            DeyeModbusDataType.Int16 => TryReadInt16(data, out rawValue),
            DeyeModbusDataType.UInt32 => TryReadUInt32(data, out rawValue),
            DeyeModbusDataType.Int32 => TryReadInt32(data, out rawValue),
            _ => false
        };
    }

    private bool TryReadUInt16(ReadOnlySpan<byte> data, out double rawValue)
    {
        rawValue = 0d;

        if (Length != 1 || data.Length < 2)
        {
            return false;
        }

        rawValue = BinaryPrimitives.ReadUInt16BigEndian(data);
        return true;
    }

    private bool TryReadInt16(ReadOnlySpan<byte> data, out double rawValue)
    {
        rawValue = 0d;

        if (Length != 1 || data.Length < 2)
        {
            return false;
        }

        rawValue = BinaryPrimitives.ReadInt16BigEndian(data);
        return true;
    }

    private bool TryReadUInt32(ReadOnlySpan<byte> data, out double rawValue)
    {
        rawValue = 0d;

        if (Length != 2 || data.Length < 4)
        {
            return false;
        }

        var highWord = BinaryPrimitives.ReadUInt16BigEndian(data);
        var lowWord = BinaryPrimitives.ReadUInt16BigEndian(data[2..]);
        rawValue = ((uint)highWord << 16) | lowWord;
        return true;
    }

    private bool TryReadInt32(ReadOnlySpan<byte> data, out double rawValue)
    {
        rawValue = 0d;

        if (Length != 2 || data.Length < 4)
        {
            return false;
        }

        var highWord = BinaryPrimitives.ReadUInt16BigEndian(data);
        var lowWord = BinaryPrimitives.ReadUInt16BigEndian(data[2..]);
        var combined = ((uint)highWord << 16) | lowWord;
        rawValue = unchecked((int)combined);
        return true;
    }

    private static DeyeModbusDataType ParseDataType(string raw)
    {
        var normalized = raw.Trim().ToLowerInvariant();

        return normalized switch
        {
            "uint16" => DeyeModbusDataType.UInt16,
            "int16" => DeyeModbusDataType.Int16,
            "uint32" => DeyeModbusDataType.UInt32,
            "int32" => DeyeModbusDataType.Int32,
            _ => DeyeModbusDataType.Unknown
        };
    }

    private enum DeyeModbusDataType
    {
        Unknown = 0,
        UInt16,
        Int16,
        UInt32,
        Int32
    }
}
