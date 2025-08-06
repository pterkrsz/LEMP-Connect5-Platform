namespace LEMP.Domain.Audit;

public class AuditLog
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string? User { get; set; }
    public string? Role { get; set; }
    public string? Path { get; set; }
    public string? Method { get; set; }
    public int StatusCode { get; set; }
    public string? IpAddress { get; set; }
    public double DurationMs { get; set; }
}
