namespace LEMP.Domain.DataPoints;

public class InverterDataPoint
{
    public string BuildingId { get; set; } = string.Empty;
    public string InverterId { get; set; } = string.Empty;

    public double PowerActive { get; set; }
    public double PowerReactive { get; set; }
    public double Frequency { get; set; }
    public double VoltageL1 { get; set; }
    public double VoltageL2 { get; set; }
    public double VoltageL3 { get; set; }
    public double CurrentL1 { get; set; }
    public double CurrentL2 { get; set; }
    public double CurrentL3 { get; set; }

    public DateTime Timestamp { get; set; }
}
