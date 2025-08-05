namespace LEMP.Domain.SmartMeter;

public class SmartMeterState
{
    public double VoltageLineToNeutral { get; set; }        // Register 0
    public double Current { get; set; }                     // Register 6
    public double ActivePower { get; set; }                 // Register 12
    public double ApparentPower { get; set; }               // Register 18
    public double ReactivePower { get; set; }               // Register 24
    public double PowerFactor { get; set; }                 // Register 30
    public double Frequency { get; set; }                   // Register 70
    public double ImportedActiveEnergy { get; set; }        // Register 72
    public double ExportedActiveEnergy { get; set; }        // Register 74
    public double ImportedReactiveEnergy { get; set; }      // Register 76
    public double ExportedReactiveEnergy { get; set; }      // Register 78
    public double TotalActiveEnergy { get; set; }           // Register 342
    public bool SmartMeterAlive { get; set; } = false;      // true if all reads succeed
}
