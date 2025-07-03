namespace LEMP.Domain;

public class Measurement
{
    public int Id { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, double> Values { get; set; } = new();
}
