namespace LEMP.Domain.DataPoints;

public class MetaDataPoint
{
    public string BuildingId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;

    public string FirmwareVersion { get; set; } = string.Empty;
    public bool CommunicationStatus { get; set; }
    public DateTime LastUpdateTime { get; set; }

    public DateTime Timestamp { get; set; }
}
