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

        try
        {
            var slice = rawData[..ByteLength];
            if (!TryReadRawValue(slice, out var rawValue))
            {
                return false;
            }

            value = rawValue;
        }
        catch
        {
            return false;
        }

        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return false;
        }

        value *= Scale;
        return true;
    }

    private bool TryReadRawValue(ReadOnlySpan<byte> data, out double rawValue)
    {
        rawValue = 0d;

        var expectedByteLength = _dataType switch
        {
            DeyeModbusDataType.UInt16 or DeyeModbusDataType.Int16 => 2,
            DeyeModbusDataType.UInt32 or DeyeModbusDataType.Int32 => 4,
            _ => 0
        };

        if (expectedByteLength == 0 || ByteLength != expectedByteLength || data.Length < expectedByteLength)
        {
            return false;
        }

        rawValue = _dataType switch
        {
            DeyeModbusDataType.UInt16 => BinaryPrimitives.ReadUInt16BigEndian(data),
            DeyeModbusDataType.Int16 => BinaryPrimitives.ReadInt16BigEndian(data),
            DeyeModbusDataType.UInt32 => BinaryPrimitives.ReadUInt32BigEndian(data),
            DeyeModbusDataType.Int32 => BinaryPrimitives.ReadInt32BigEndian(data),
            _ => double.NaN
        };

        return !double.IsNaN(rawValue) && !double.IsInfinity(rawValue);
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
