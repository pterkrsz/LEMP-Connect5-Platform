namespace LEMP.Domain.Inverter;

public class InverterSnapshot
{
    public bool InverterAlive { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public Dictionary<string, Dictionary<string, InverterRegisterValue>> Groups { get; } =
        new(StringComparer.OrdinalIgnoreCase);
}
