using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using LEMP.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace LEMP.Infrastructure.Services;

public class InfluxDataPointService : IDataPointService
{
    private readonly IWriteApiAsync _writeApi;
    private readonly string _bucket;
    private readonly string _org;
    private readonly ILogger<InfluxDataPointService>? _logger;

    public InfluxDataPointService(IWriteApiAsync writeApi, string bucket, string org, ILogger<InfluxDataPointService>? logger = null)
    {
        _writeApi = writeApi;
        _bucket = bucket;
        _org = org;
        _logger = logger;
    }

    public async Task WriteAsync<T>(T point)
    {
        try
        {
            await _writeApi.WriteMeasurementAsync(point, WritePrecision.Ns, _bucket, _org);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to write measurement to InfluxDB");
            throw;
        }
    }
}
