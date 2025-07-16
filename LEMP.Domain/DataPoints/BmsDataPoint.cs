using InfluxDB.Client.Core;
using InfluxDB.Client.Api.Domain;

namespace LEMP.Domain.DataPoints;

[Measurement("bms_data")]
public class BmsDataPoint
{
    [Column("BuildingId", IsTag = true)]
    public string BuildingId { get; set; } = string.Empty;
    [Column("BatteryId", IsTag = true)]
    public string BatteryId { get; set; } = string.Empty;

    [Column("charge_current")] public double ChargeCurrent { get; set; }
    [Column("discharge_current")] public double DischargeCurrent { get; set; }
    [Column("temperature_avg")] public double Temperature { get; set; }
    [Column("active_protection")] public int ActiveProtection { get; set; }
    [Column("error_code")] public int ErrorCode { get; set; }
    [Column("soc_limit")] public double SocLimit { get; set; }
    [Column("charge_discharge_ok")] public bool ChargeDischargeOk { get; set; }
    [Column("relay_status")] public bool RelayStatus { get; set; }
    [Column("cell_balancing")] public bool CellBalancingStatus { get; set; }

    [Column(IsTimestamp = true)]
    public DateTime Timestamp { get; set; }
}
