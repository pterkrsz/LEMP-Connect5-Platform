namespace LEMP.Domain.DataPoints;

public class BmsDataPoint
{
    public string BuildingId { get; set; } = string.Empty;
    public string BatteryId { get; set; } = string.Empty;

    public double ChargeCurrent { get; set; }
    public double DischargeCurrent { get; set; }
    public double Temperature { get; set; }
    public int ActiveProtection { get; set; }
    public int ErrorCode { get; set; }
    public double SocLimit { get; set; }
    public bool ChargeDischargeOk { get; set; }
    public bool RelayStatus { get; set; }
    public bool CellBalancingStatus { get; set; }

    public DateTime Timestamp { get; set; }
}
