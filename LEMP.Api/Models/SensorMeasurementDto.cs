namespace LEMP.Api.Models
{
    // DTO representing sensor measurement data
    public class SensorMeasurementDto
    {
        public string DeviceId { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
