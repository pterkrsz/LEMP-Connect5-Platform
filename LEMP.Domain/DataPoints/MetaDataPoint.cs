using InfluxDB.Client.Core;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

namespace LEMP.Domain.DataPoints;

[Measurement("meta_data")]
public class MetaDataPoint
{
    [Tag] public string BuildingId { get; set; } = string.Empty;
    [Tag] public string DeviceId { get; set; } = string.Empty;

    [Field("firmware_version")]   public string FirmwareVersion { get; set; } = string.Empty;
    [Field("comm_status")]        public bool CommunicationStatus { get; set; }
    [Field("last_update_time")]   public DateTime LastUpdateTime { get; set; }

    [Timestamp(WritePrecision.Ns)]
    public DateTime Timestamp { get; set; }
}
