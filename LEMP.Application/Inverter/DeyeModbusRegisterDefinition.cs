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
        _normalizedDataType = dataType.Trim().ToLowerInvariant();
    }

    private readonly string _normalizedDataType;

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

            value = _normalizedDataType switch
            {
                "uint16" => BinaryPrimitives.ReadUInt16BigEndian(slice),
                "int16" => BinaryPrimitives.ReadInt16BigEndian(slice),
                "uint32" => BinaryPrimitives.ReadUInt32BigEndian(slice),
                "int32" => BinaryPrimitives.ReadInt32BigEndian(slice),
                "single" or "float" or "float32" =>
                    BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32BigEndian(slice)),
                "double" or "float64" =>
                    BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64BigEndian(slice)),
                "bool" or "boolean" => slice[^1] != 0 ? 1d : 0d,
                _ => double.NaN
            };
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
}
