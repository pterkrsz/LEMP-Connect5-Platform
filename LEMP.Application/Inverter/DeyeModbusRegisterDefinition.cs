using System;

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

        Span<byte> buffer = stackalloc byte[ByteLength];
        rawData[..ByteLength].CopyTo(buffer);
        buffer.Reverse();

        try
        {
            value = _normalizedDataType switch
            {
                "uint16" => BitConverter.ToUInt16(buffer),
                "int16" => BitConverter.ToInt16(buffer),
                "uint32" => BitConverter.ToUInt32(buffer),
                "int32" => BitConverter.ToInt32(buffer),
                "single" or "float" or "float32" => BitConverter.ToSingle(buffer),
                "double" or "float64" => BitConverter.ToDouble(buffer),
                "bool" or "boolean" => buffer[^1] != 0 ? 1d : 0d,
                _ => double.NaN
            };
        }
        catch
        {
            return false;
        }

        if (double.IsNaN(value))
        {
            return false;
        }

        value *= Scale;
        return true;
    }
}
