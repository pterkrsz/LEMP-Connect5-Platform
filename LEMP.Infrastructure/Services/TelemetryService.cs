using InfluxDB3.Client;
using InfluxDB3.Client.Write;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace LEMP.Infrastructure.Services;

public class TelemetryService
{
    private readonly InfluxDBClient _client;
    private readonly ILogger<TelemetryService> _logger;

    public TelemetryService(InfluxDBClient client, ILogger<TelemetryService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task SendInverterReadingAsync(
        string buildingId,
        string inverterId,
        double powerActive,
        double powerReactive,
        double frequency,
        double voltageL1,
        double voltageL2,
        double voltageL3,
        double currentL1,
        double currentL2,
        double currentL3,
        DateTime timestamp)
    {
        var point = PointData.Measurement("inverter_data")
            .SetTag("BuildingId", buildingId)
            .SetTag("InverterId", inverterId)
            .SetField("power_active", powerActive)
            .SetField("power_reactive", powerReactive)
            .SetField("Frequency", frequency)
            .SetField("voltage_l1", voltageL1)
            .SetField("voltage_l2", voltageL2)
            .SetField("voltage_l3", voltageL3)
            .SetField("current_l1", currentL1)
            .SetField("current_l2", currentL2)
            .SetField("current_l3", currentL3)
            .SetTimestamp(timestamp);

        await _client.WritePointAsync(point);
        _logger.LogInformation("Inverter data written for {InverterId}", inverterId);
    }
}
