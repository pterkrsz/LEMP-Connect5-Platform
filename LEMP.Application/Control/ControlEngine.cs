using LEMP.Domain.Control;

namespace LEMP.Application.Control;

/// <summary>
/// Simple evaluation engine for determining the system control state.
/// </summary>
public class ControlEngine
{
    private ControlState _lastState = ControlState.Idle;
    private DateTime _lastStateTime = DateTime.MinValue;
    private ControlState? _manualOverride;

    public BatteryState? LastBattery { get; private set; }
    public InverterState? LastInverter { get; private set; }
    public SmartMeterState? LastSmartMeter { get; private set; }

    private readonly List<string> _logs = new();

    public double ShutoffThresholdSOC { get; set; } = 5.0;
    public double RestartThresholdSOC { get; set; } = 10.0;

    /// <summary>
    /// Evaluates the system state based on incoming measurements.
    /// </summary>
    public ControlState EvaluateState(BatteryState battery, InverterState inverter, SmartMeterState meter)
    {
        LastBattery = battery;
        LastInverter = inverter;
        LastSmartMeter = meter;

        var state = _manualOverride ?? ComputeState(battery, inverter, meter);
        _lastState = state;
        _lastStateTime = DateTime.UtcNow;
        _logs.Add($"State evaluated: {state} at {_lastStateTime:O}");
        return state;
    }

    private ControlState ComputeState(BatteryState b, InverterState i, SmartMeterState m)
    {
        if ((b.BatteryAlarms != null && b.BatteryAlarms.Length > 0) ||
            (i.InverterAlarmCodes != null && i.InverterAlarmCodes.Length > 0))
        {
            return ControlState.Error;
        }

        if (b.BatterySOC < ShutoffThresholdSOC)
        {
            return ControlState.Error;
        }

        if (b.BatterySOC < 20 && b.BatteryChargeAllowed && b.BatteryCanCharge)
        {
            return ControlState.Charge;
        }

        if (b.BatterySOC > 80 && b.BatteryDischargeAllowed && b.BatteryCanDischarge)
        {
            return ControlState.Discharge;
        }

        return ControlState.Idle;
    }

    public (ControlState State, DateTime Time) GetLastState() => (_lastState, _lastStateTime);

    public void RequestBatteryReset() => _logs.Add("Battery reset requested");

    public void ApplyLastState() => _logs.Add($"Applied state: {_lastState}");

    public void SetOverride(ControlState state)
    {
        _manualOverride = state;
        _logs.Add($"Manual override set: {state}");
    }

    public void ClearOverride()
    {
        _manualOverride = null;
        _logs.Add("Manual override cleared");
    }

    public IReadOnlyList<string> GetLogs() => _logs.AsReadOnly();
}
