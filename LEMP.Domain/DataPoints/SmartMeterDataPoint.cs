using InfluxDB.Client.Core;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

namespace LEMP.Domain.DataPoints;

[Measurement("smartmeter_data")]
public class SmartMeterDataPoint
{
    [Tag] public string BuildingId { get; set; } = string.Empty;
    [Tag] public string MeterId { get; set; } = string.Empty;

    [Field("total_import_energy")] public double TotalImportEnergy { get; set; }
    [Field("total_export_energy")] public double TotalExportEnergy { get; set; }
    [Field("current_power")]       public double CurrentPower { get; set; }
    [Field("reactive_power")]      public double ReactivePower { get; set; }
    [Field("power_factor")]        public double PowerFactor { get; set; }
    [Field("voltage_l1")]         public double VoltageL1 { get; set; }
    [Field("voltage_l2")]         public double VoltageL2 { get; set; }
    [Field("voltage_l3")]         public double VoltageL3 { get; set; }
    [Field("current_l1")]         public double CurrentL1 { get; set; }
    [Field("current_l2")]         public double CurrentL2 { get; set; }
    [Field("current_l3")]         public double CurrentL3 { get; set; }
    [Field("phase_sequence")]     public string PhaseSequence { get; set; } = string.Empty;
    [Field("power_direction")]    public string PowerDirection { get; set; } = string.Empty;

    [Timestamp(WritePrecision.Ns)]
    public DateTime Timestamp { get; set; }
}
