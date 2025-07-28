namespace LEMP.Api.Models
{
    // DTO representing meter measurement data
    public class MeterMeasurementDto
    {
        public DateTime Timestamp { get; set; }
        public double L1Active { get; set; }
        public double L1Reactive { get; set; }
        public double L1Current { get; set; }
    }
}
