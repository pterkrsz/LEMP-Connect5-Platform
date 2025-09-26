namespace LEMP.Domain.Inverter;

public sealed record InverterRegisterValue(
    double Value,
    double RawValue,
    string DataType,
    double Scale,
    string? Unit);
