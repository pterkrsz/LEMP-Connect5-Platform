using InfluxDB3.Client;
using InfluxDB3.Client.Query;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace LEMP.Infrastructure.Services;

public class InfluxDbInitializer
{
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
        using var http = new HttpClient { BaseAddress = new Uri(_endpointUrl) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);

        try
        {
            var orgPayload = new { name = _organization };
            var resp = await http.PostAsJsonAsync("/api/v3/organizations", orgPayload);
            if (!resp.IsSuccessStatusCode && resp.StatusCode != HttpStatusCode.Conflict)
            {
                resp.EnsureSuccessStatusCode();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create organization");
            throw;
        }

        try
        {
            var bucketPayload = new { name = _bucket, retentionDays = (int)_retentionPeriod.TotalDays };
            var resp = await http.PostAsJsonAsync("/api/v3/buckets", bucketPayload);
            if (!resp.IsSuccessStatusCode && resp.StatusCode != HttpStatusCode.Conflict)
            {
                resp.EnsureSuccessStatusCode();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create bucket");
            throw;
        }

        using var sqlClient = new InfluxDBClient(_endpointUrl, token: _authToken);

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
                await foreach (var _ in sqlClient.Query(sql, QueryType.SQL, _bucket)) { }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to execute statement: {Sql}", sql);
                throw;
            }
        }
    }
}


