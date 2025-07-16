using InfluxDB.Client.Core;
using InfluxDB.Client.Api.Domain;

namespace LEMP.Domain.DataPoints;

[Measurement("smartmeter_data")]
public class SmartMeterDataPoint
{
    [Column("BuildingId", IsTag = true)]
    public string BuildingId { get; set; } = string.Empty;
    [Column("MeterId", IsTag = true)]
    public string MeterId { get; set; } = string.Empty;

    [Column("total_import_energy")] public double TotalImportEnergy { get; set; }
    [Column("total_export_energy")] public double TotalExportEnergy { get; set; }
    [Column("current_power")] public double CurrentPower { get; set; }
    [Column("reactive_power")] public double ReactivePower { get; set; }
    [Column("power_factor")] public double PowerFactor { get; set; }
    [Column("voltage_l1")] public double VoltageL1 { get; set; }
    [Column("voltage_l2")] public double VoltageL2 { get; set; }
    [Column("voltage_l3")] public double VoltageL3 { get; set; }
    [Column("current_l1")] public double CurrentL1 { get; set; }
    [Column("current_l2")] public double CurrentL2 { get; set; }
    [Column("current_l3")] public double CurrentL3 { get; set; }
    [Column("phase_sequence")] public string PhaseSequence { get; set; } = string.Empty;
    [Column("power_direction")] public string PowerDirection { get; set; } = string.Empty;

    [Column(IsTimestamp = true)]
    public DateTime Timestamp { get; set; }
}
