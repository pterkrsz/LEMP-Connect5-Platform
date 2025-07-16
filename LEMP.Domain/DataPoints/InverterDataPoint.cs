using InfluxDB.Client.Core;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

namespace LEMP.Domain.DataPoints;

[Measurement("inverter_data")]
public class InverterDataPoint
{
    [Tag] public string BuildingId { get; set; } = string.Empty;
    [Tag] public string InverterId { get; set; } = string.Empty;

    [Field("power_active")]   public double PowerActive { get; set; }
    [Field("power_reactive")] public double PowerReactive { get; set; }
    [Field]                   public double Frequency { get; set; }
    [Field("voltage_l1")]     public double VoltageL1 { get; set; }
    [Field("voltage_l2")]     public double VoltageL2 { get; set; }
    [Field("voltage_l3")]     public double VoltageL3 { get; set; }
    [Field("current_l1")]     public double CurrentL1 { get; set; }
    [Field("current_l2")]     public double CurrentL2 { get; set; }
    [Field("current_l3")]     public double CurrentL3 { get; set; }

    [Timestamp(WritePrecision.Ns)]
    public DateTime Timestamp { get; set; }
}
