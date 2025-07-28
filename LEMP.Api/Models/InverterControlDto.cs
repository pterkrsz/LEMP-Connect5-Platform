namespace LEMP.Api.Models
{
    // DTO for inverter control commands
    public class InverterControlDto
    {
        public string MethodName { get; set; } = string.Empty;
        public required Dictionary<string, string> Payload { get; set; } = new();
        public int Timeout { get; set; }
    }
}
