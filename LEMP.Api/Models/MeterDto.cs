namespace LEMP.Api.Models
{
    // DTO representing meter master data
    public class MeterDto
    {
        public string MeterId { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public GpsDto Gps { get; set; } = new();
    }
}
