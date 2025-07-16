using InfluxDB.Client.Core;
using InfluxDB.Client.Api.Domain;

namespace LEMP.Domain.DataPoints;

[Measurement("meta_data")]
public class MetaDataPoint
{
    [Column("BuildingId", IsTag = true)]
    public string BuildingId { get; set; } = string.Empty;
    [Column("DeviceId", IsTag = true)]
    public string DeviceId { get; set; } = string.Empty;

    [Column("firmware_version")] public string FirmwareVersion { get; set; } = string.Empty;
    [Column("comm_status")] public bool CommunicationStatus { get; set; }
    [Column("last_update_time")] public DateTime LastUpdateTime { get; set; }

    [Column(IsTimestamp = true)]
    public DateTime Timestamp { get; set; }
}
