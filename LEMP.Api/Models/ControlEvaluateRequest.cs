using LEMP.Domain.Control;

namespace LEMP.Api.Models;

/// <summary>
/// Request payload containing all measurement states for evaluation.
/// </summary>
public class ControlEvaluateRequest
{
    public required BatteryState Battery { get; set; }
    public required InverterState Inverter { get; set; }
    public required SmartMeterState SmartMeter { get; set; }
}
