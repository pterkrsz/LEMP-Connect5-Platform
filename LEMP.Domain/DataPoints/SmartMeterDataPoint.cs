namespace LEMP.Domain.DataPoints;

public class SmartMeterDataPoint
{
    public string BuildingId { get; set; } = string.Empty;
    public string MeterId { get; set; } = string.Empty;

    public double TotalImportEnergy { get; set; }
    public double TotalExportEnergy { get; set; }
    public double CurrentPower { get; set; }
    public double ReactivePower { get; set; }
    public double PowerFactor { get; set; }
    public double VoltageL1 { get; set; }
    public double VoltageL2 { get; set; }
    public double VoltageL3 { get; set; }
    public double CurrentL1 { get; set; }
    public double CurrentL2 { get; set; }
    public double CurrentL3 { get; set; }
    public string PhaseSequence { get; set; } = string.Empty;
    public string PowerDirection { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
