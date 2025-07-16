using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using LEMP.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace LEMP.Infrastructure.Services;

public class InfluxDataPointService : IDataPointService
{
    private readonly InfluxDBClient _client;
    private readonly string _bucket;
    private readonly string _organization;
    private readonly ILogger<InfluxDataPointService>? _logger;

    public InfluxDataPointService(InfluxDBClient client, string bucket, string organization, ILogger<InfluxDataPointService>? logger = null)
    {
        _client = client;
        _bucket = bucket;
        _organization = organization;
        _logger = logger;
    }

    public async Task WriteAsync<T>(T point)
    {
        try
        {
            using var write = _client.GetWriteApiAsync();
            await write.WriteMeasurementAsync(point, WritePrecision.Ns, _bucket, _organization);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to write measurement to InfluxDB");
            throw;
        }
    }
}
