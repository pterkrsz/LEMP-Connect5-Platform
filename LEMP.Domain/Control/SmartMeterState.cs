namespace LEMP.Domain.Control;

/// <summary>
/// Represents smart meter measurements.
/// </summary>
public class SmartMeterState
{
    public double GridImportPowerW { get; set; }
    public double GridExportPowerW { get; set; }
    public double GridVoltageL1 { get; set; }
    public double GridVoltageL2 { get; set; }
    public double GridVoltageL3 { get; set; }
    public double GridCurrentL1 { get; set; }
    public double GridCurrentL2 { get; set; }
    public double GridCurrentL3 { get; set; }
    public double GridActivePowerTotal { get; set; }
    public double GridReactivePowerTotal { get; set; }
    public double GridApparentPowerTotal { get; set; }
    public double GridFrequencyHz { get; set; }
    public double GridEnergyImportedWh { get; set; }
    public double GridEnergyExportedWh { get; set; }
    public double PowerFactor { get; set; }
    public bool SmartMeterAlive { get; set; }
}
