using System;

namespace LEMP.Domain.Control;

/// <summary>
/// Represents current Battery Management System information.
/// </summary>
public class BatteryState
{
    public double BatteryNominalCapacityAh { get; set; }
    public double BatteryUsableCapacityPercent { get; set; }
    public double BatterySOC { get; set; }
    public double BatteryDOD { get; set; }
    public double BatteryVoltage { get; set; }
    public double BatteryCurrent { get; set; }
    public double BatteryPowerW { get; set; }
    public double BatteryTemperatureMax { get; set; }
    public double BatteryTemperatureMin { get; set; }
    public double[]? BatteryCellVoltages { get; set; }
    public bool BatteryBalanceActive { get; set; }
    public bool BatteryChargeAllowed { get; set; }
    public bool BatteryDischargeAllowed { get; set; }
    public string[]? BatteryAlarms { get; set; }
    public BatteryStatus BatteryStatus { get; set; }
    public bool BatteryCanCharge { get; set; }
    public bool BatteryCanDischarge { get; set; }
    public double BatteryShutoffThresholdSoc { get; set; }
    public double BatteryRestartThresholdSoc { get; set; }
    public double BatteryChargePowerLimitW { get; set; }
    public double BatteryDischargePowerLimitW { get; set; }
    public bool BatteryCommAlive { get; set; }
    public bool BatteryIsCalibrated { get; set; }
    public double BatteryAllowedChargeVoltageMax { get; set; }
    public double BatteryAllowedDischargeVoltageMin { get; set; }
    public bool BatteryResetRequest { get; set; }
    public bool BatteryForceStop { get; set; }
}
