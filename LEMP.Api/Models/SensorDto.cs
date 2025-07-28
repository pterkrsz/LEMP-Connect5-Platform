namespace LEMP.Api.Models
{
    // DTO representing IoT sensor master data
    public class SensorDto
    {
        public string DeviceId { get; set; } = string.Empty;
        public string SensorType { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
    }
}
