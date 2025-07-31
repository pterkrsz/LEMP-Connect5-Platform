namespace LEMP.Domain.Control;

// Battery overall status values
public enum BatteryStatus
{
    Unknown,
    Normal,
    Error,
    Balancing,
    Shutdown
}

// Inverter operating modes
public enum InverterMode
{
    Unknown,
    GridTied,
    OffGrid,
    Charge,
    Standby
}

// Resulting control state from the control engine
public enum ControlState
{
    Idle,
    Charge,
    Discharge,
    Error
}
