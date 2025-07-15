using InfluxDB3.Client;
using InfluxDB3.Client.Write;
using LEMP.Application.DTOs;
using LEMP.Application.Interfaces;

namespace LEMP.Infrastructure.Services;

public class InfluxMeasurementService : IMeasurementService
{
    private readonly IInfluxDBClient _client;

    public InfluxMeasurementService(IInfluxDBClient client)
    {
        _client = client;
    }

    public async Task AddMeasurementAsync(MeasurementDto dto)
    {
        var point = PointData.Measurement("measurements")
            .SetTag("source_type", dto.SourceType)
            .SetTag("source_id", dto.SourceId)
            .SetTimestamp(dto.Timestamp);

        foreach (var kv in dto.Values)
        {
            point = point.SetField(kv.Key, kv.Value);
        }

        await _client.WritePointAsync(point);
    }

    public async Task<IEnumerable<MeasurementDto>> GetAllAsync()
    {
        const string sql = "SELECT * FROM measurements";
        var result = new List<MeasurementDto>();

        await foreach (var point in _client.QueryPoints(sql))
        {
            var dto = new MeasurementDto
            {
                SourceType = point.GetTag("source_type") ?? string.Empty,
                SourceId = point.GetTag("source_id") ?? string.Empty,
                Timestamp = point.GetTimestamp().HasValue
                    ? DateTime.UnixEpoch.AddTicks((long)(point.GetTimestamp()!.Value / 100))
                    : DateTime.UtcNow,
                Values = new Dictionary<string, double>()
            };

            foreach (var name in point.GetFieldNames())
            {
                var valObj = point.GetField(name);
                if (valObj != null && double.TryParse(valObj.ToString(), out var val))
                {
                    dto.Values[name] = val;
                }
            }

            result.Add(dto);
        }

        return result;
    }
}
