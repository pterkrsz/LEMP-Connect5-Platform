namespace LEMP.Api.Models
{
    // DTO representing institution master data
    public class InstitutionDto
    {
        public string InstitutionId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public GpsDto Gps { get; set; } = new();
        public ContactDto Contact { get; set; } = new();
    }

    public class GpsDto
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
    }

    public class ContactDto
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
