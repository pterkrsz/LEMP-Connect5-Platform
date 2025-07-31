namespace LEMP.Domain.Control;

/// <summary>
/// Represents inverter telemetry and limits.
/// </summary>
public class InverterState
{
    public double InverterOutputPowerW { get; set; }
    public double InverterInputPowerW { get; set; }
    public InverterMode InverterMode { get; set; }
    public string? InverterStatus { get; set; }
    public bool InverterGridConnected { get; set; }
    public double InverterBatteryPowerW { get; set; }
    public double InverterPVPowerW { get; set; }
    public double InverterAllowedChargePowerW { get; set; }
    public double InverterAllowedDischargePowerW { get; set; }
    public double InverterSetPowerW { get; set; }
    public double InverterReactivePowerVar { get; set; }
    public double InverterFrequencyHz { get; set; }
    public string[]? InverterAlarmCodes { get; set; }
}
