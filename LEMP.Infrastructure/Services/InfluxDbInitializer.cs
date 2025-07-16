using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.IO;
using InfluxDB3.Client;
using InfluxDB3.Client.Query;
using Microsoft.Extensions.Logging;

namespace LEMP.Infrastructure.Services;

public class InfluxDbInitializer
{
    private const int SchemaVersion = 1;
    private static readonly string StateFilePath = Path.Combine(AppContext.BaseDirectory, "influxdb.state");
    private readonly string _endpointUrl;
    private readonly string _authToken;
    private readonly string _organization;
    private readonly string _bucket;
    private readonly TimeSpan _retentionPeriod;
    private readonly ILogger<InfluxDbInitializer>? _logger;

    public InfluxDbInitializer(
        string endpointUrl,
        string authToken,
        string organization,
        string bucket,
        TimeSpan retentionPeriod,
        ILogger<InfluxDbInitializer>? logger = null)
    {
        _endpointUrl = endpointUrl;
        _authToken = authToken;
        _organization = organization;
        _bucket = bucket;
        _retentionPeriod = retentionPeriod;
        _logger = logger;
    }

    public async Task EnsureDatabaseStructureAsync()
    {
        if (File.Exists(StateFilePath))
        {
            try
            {
                var content = await File.ReadAllTextAsync(StateFilePath);
                if (int.TryParse(content, out var version) && version >= SchemaVersion)
                {
                    _logger?.LogInformation("InfluxDB already initialized with schema version {Version}", version);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to read initialization state");
            }
        }

        using var http = new HttpClient { BaseAddress = new Uri(_endpointUrl) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);

        var orgUri = $"/api/v3/configure/organization?org={Uri.EscapeDataString(_organization)}";
        await CreateIfNotExistsAsync(http, orgUri, "organization");

        var days = (int)Math.Ceiling(_retentionPeriod.TotalDays);
        var bucketUri = $"/api/v3/configure/database?db={Uri.EscapeDataString(_bucket)}&retentionDays={days}";
        await CreateIfNotExistsAsync(http, bucketUri, "bucket");

        using var client = new InfluxDBClient(_endpointUrl, token: _authToken, database: _bucket);

        var statements = new[]
        {
            @"CREATE TABLE IF NOT EXISTS inverter_data (
                time TIMESTAMPTZ NOT NULL,
                BuildingId TEXT DIMENSION,
                InverterId TEXT DIMENSION,
                power_active DOUBLE,
                power_reactive DOUBLE,
                Frequency DOUBLE,
                voltage_l1 DOUBLE,
                voltage_l2 DOUBLE,
                voltage_l3 DOUBLE,
                current_l1 DOUBLE,
                current_l2 DOUBLE,
                current_l3 DOUBLE)",
            @"CREATE TABLE IF NOT EXISTS bms_data (
                time TIMESTAMPTZ NOT NULL,
                BuildingId TEXT DIMENSION,
                BatteryId TEXT DIMENSION,
                charge_current DOUBLE,
                discharge_current DOUBLE,
                temperature_avg DOUBLE,
                active_protection BIGINT,
                error_code BIGINT,
                soc_limit DOUBLE,
                charge_discharge_ok BOOLEAN,
                relay_status BOOLEAN,
                cell_balancing BOOLEAN)",
            @"CREATE TABLE IF NOT EXISTS smartmeter_data (
                time TIMESTAMPTZ NOT NULL,
                BuildingId TEXT DIMENSION,
                MeterId TEXT DIMENSION,
                total_import_energy DOUBLE,
                total_export_energy DOUBLE,
                current_power DOUBLE,
                reactive_power DOUBLE,
                power_factor DOUBLE,
                voltage_l1 DOUBLE,
                voltage_l2 DOUBLE,
                voltage_l3 DOUBLE,
                current_l1 DOUBLE,
                current_l2 DOUBLE,
                current_l3 DOUBLE,
                phase_sequence STRING,
                power_direction STRING)",
            @"CREATE TABLE IF NOT EXISTS meta_data (
                time TIMESTAMPTZ NOT NULL,
                BuildingId TEXT DIMENSION,
                DeviceId TEXT DIMENSION,
                firmware_version STRING,
                comm_status BOOLEAN,
                last_update_time TIMESTAMPTZ)"
        };

        foreach (var sql in statements)
        {
            try
            {
                await foreach (var _ in client.Query(sql, QueryType.SQL, _bucket)) { }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to execute statement: {Sql}", sql);
            }
        }

        try
        {
            await File.WriteAllTextAsync(StateFilePath, SchemaVersion.ToString());
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to write initialization state");
        }
    }

    private async Task CreateIfNotExistsAsync(HttpClient http, string requestUri, string resourceName)
    {
        using var response = await http.PostAsync(requestUri, null);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            _logger?.LogInformation("{Resource} already exists", resourceName);
            return;
        }

        if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created)
        {
            _logger?.LogInformation("{Resource} created", resourceName);
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        _logger?.LogError("Failed to create {Resource}: {Status} - {Body}", resourceName, response.StatusCode, body);
        response.EnsureSuccessStatusCode();
    }
}
