using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using InfluxDB3.Client;
using InfluxDB3.Client.Query;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace LEMP.Infrastructure.Services;

public class InfluxDbInitializer
{
    private const int SchemaVersion = 1;
    private static readonly string StateFilePath = Path.Combine(AppContext.BaseDirectory, "influxdb.state");
    private readonly InfluxDBClient _client;
    private readonly string _organization;
    private readonly string _bucket;
    private readonly TimeSpan _retentionPeriod;
    private readonly ILogger<InfluxDbInitializer>? _logger;

        public InfluxDbInitializer(
            InfluxDBClient client,
            string bucket,
            string organization,
            TimeSpan retentionPeriod,
            ILogger<InfluxDbInitializer>? logger)
        {
            _client = client;
            _organization = organization;
            _bucket = bucket;
            _retentionDays = retentionDays;
            _logger = logger;
        }

    private (string Host, string Token) GetConnectionInfo()
    {
        var configField = typeof(InfluxDBClient).GetField("_config", BindingFlags.NonPublic | BindingFlags.Instance);
        var config = configField?.GetValue(_client);
        if (config is null)
            throw new InvalidOperationException("Cannot access InfluxDB client configuration");
        var hostProp = config.GetType().GetProperty("Host");
        var tokenProp = config.GetType().GetProperty("Token");
        var host = (string?)hostProp?.GetValue(config) ?? throw new InvalidOperationException("Host missing");
        var token = (string?)tokenProp?.GetValue(config) ?? string.Empty;
        return (host.TrimEnd('/'), token);
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

        var (host, token) = GetConnectionInfo();
        using var http = new HttpClient { BaseAddress = new Uri(host) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // 2) Bucket l�trehoz�sa SQL-el
            var createBucket = $"CREATE BUCKET IF NOT EXISTS \"{_bucket}\" RETENTION {_retentionDays}d";
            await ExecuteCreateAsync(client, createBucket, "Bucket");

        var days = (int)Math.Ceiling(_retentionPeriod.TotalDays);
        var bucketUri = $"/api/v3/configure/database?db={Uri.EscapeDataString(_bucket)}&retentionDays={days}";
        await CreateIfNotExistsAsync(http, bucketUri, "bucket");

        // 3. Ensure tables exist via SQL
        var statements = new[]
            {
                ["inverter_data"] = @"
                    CREATE TABLE IF NOT EXISTS inverter_data (
                        time           TIMESTAMPTZ NOT NULL,
                        BuildingId     TEXT DIMENSION,
                        InverterId     TEXT DIMENSION,
                        power_active   DOUBLE,
                        power_reactive DOUBLE
                    )",
                ["bms_data"] = @"
                    CREATE TABLE IF NOT EXISTS bms_data (
                        time               TIMESTAMPTZ NOT NULL,
                        BuildingId         TEXT DIMENSION,
                        BatteryId          TEXT DIMENSION,
                        charge_current     DOUBLE,
                        discharge_current  DOUBLE
                    )",
                ["smartmeter_data"] = @"
                    CREATE TABLE IF NOT EXISTS smartmeter_data (
                        time               TIMESTAMPTZ NOT NULL,
                        BuildingId         TEXT DIMENSION,
                        MeterId            TEXT DIMENSION,
                        total_import_energy DOUBLE,
                        total_export_energy DOUBLE
                    )",
                ["meta_data"] = @"
                    CREATE TABLE IF NOT EXISTS meta_data (
                        time             TIMESTAMPTZ NOT NULL,
                        BuildingId       TEXT DIMENSION,
                        DeviceId         TEXT DIMENSION,
                        firmware_version STRING
                    )"
            };

        foreach (var sql in statements)
        {
            try
            {
                await foreach (var _ in _client.Query(sql, QueryType.SQL, _bucket)) { }
            }
            catch (Exception ex)
            {
                await ExecuteCreateAsync(client, kv.Value, $"Table '{kv.Key}'");
            }
        }

        private async Task ExecuteCreateAsync(InfluxDBClient client, string sql, string resourceName)
        {
            try
            {
                await foreach (var _ in client.Query(sql, QueryType.SQL, database: _bucket))
                {
                    // csak v�grehajtjuk, nincs bels� �rt�k
                }
                _logger.LogInformation("[Init] {Res} l�trehozva vagy m�r l�tezik.", resourceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Init] Hiba k�zben: {Res}", resourceName);
                throw;
            }
        }
    }
}
