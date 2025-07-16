using InfluxDB.Client.Core;
using InfluxDB.Client.Api.Domain;

namespace LEMP.Domain.DataPoints;

[Measurement("inverter_data")]
public class InverterDataPoint
{
    [Column("BuildingId", IsTag = true)]
    public string BuildingId { get; set; } = string.Empty;
    [Column("InverterId", IsTag = true)]
    public string InverterId { get; set; } = string.Empty;

    [Column("power_active")] public double PowerActive { get; set; }
    [Column("power_reactive")] public double PowerReactive { get; set; }
    [Column("frequency")] public double Frequency { get; set; }
    [Column("voltage_l1")] public double VoltageL1 { get; set; }
    [Column("voltage_l2")] public double VoltageL2 { get; set; }
    [Column("voltage_l3")] public double VoltageL3 { get; set; }
    [Column("current_l1")] public double CurrentL1 { get; set; }
    [Column("current_l2")] public double CurrentL2 { get; set; }
    [Column("current_l3")] public double CurrentL3 { get; set; }

    [Column(IsTimestamp = true)]
    public DateTime Timestamp { get; set; }
}
