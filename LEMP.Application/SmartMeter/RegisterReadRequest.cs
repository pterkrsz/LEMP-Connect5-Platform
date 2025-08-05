using System;

namespace LEMP.Application.SmartMeter;

public class RegisterReadRequest<T>
{
    public byte SlaveId { get; init; }
    public ushort StartAddress { get; init; }
    public ushort RegisterCount { get; init; }
    public byte FunctionCode { get; init; }
    public Action<T>? OnValue { get; init; }
}
