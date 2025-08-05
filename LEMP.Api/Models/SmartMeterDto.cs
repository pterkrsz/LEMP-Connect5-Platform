namespace LEMP.Api.Models
{
    /// <summary>
    /// DTO representing a smart meter measurement returned from InfluxDB.
    /// </summary>
    public class SmartMeterDto
    {
        public DateTime Timestamp { get; set; }
        public string? Node { get; set; }
        public double Voltage { get; set; }
        public double Current { get; set; }
        public double ActivePower { get; set; }
        public double ApparentPower { get; set; }
        public double ReactivePower { get; set; }
        public double PowerFactor { get; set; }
        public double Frequency { get; set; }
        public double ImportedActiveEnergy { get; set; }
        public double ExportedActiveEnergy { get; set; }
        public double ImportedReactiveEnergy { get; set; }
        public double ExportedReactiveEnergy { get; set; }
        public double TotalActiveEnergy { get; set; }
    }
}
