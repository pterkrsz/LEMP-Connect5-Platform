using InfluxDB.Client.Core;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

namespace LEMP.Domain.DataPoints;

[Measurement("bms_data")]
public class BmsDataPoint
{
    [Tag] public string BuildingId { get; set; } = string.Empty;
    [Tag] public string BatteryId { get; set; } = string.Empty;

    [Field("charge_current")]      public double ChargeCurrent { get; set; }
    [Field("discharge_current")]   public double DischargeCurrent { get; set; }
    [Field("temperature_avg")]     public double Temperature { get; set; }
    [Field("active_protection")]   public int ActiveProtection { get; set; }
    [Field("error_code")]          public int ErrorCode { get; set; }
    [Field("soc_limit")]           public double SocLimit { get; set; }
    [Field("charge_discharge_ok")] public bool ChargeDischargeOk { get; set; }
    [Field("relay_status")]        public bool RelayStatus { get; set; }
    [Field("cell_balancing")]     public bool CellBalancingStatus { get; set; }

    [Timestamp(WritePrecision.Ns)]
    public DateTime Timestamp { get; set; }
}
