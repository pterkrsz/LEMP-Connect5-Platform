using InfluxDB.Client.Api.Domain;
using InfluxDB3.Client.Query;
using LegacyInfluxClient = InfluxDB.Client.InfluxDBClient;
using InfluxDb3Client = InfluxDB3.Client.InfluxDBClient;
using Microsoft.Extensions.Logging;

namespace LEMP.Infrastructure.Services;

public class InfluxDbInitializer
{
    private readonly string _endpointUrl;
    private readonly string _authToken;
    private readonly string _organization;
    private readonly string _bucket;
    private readonly string _database;
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
        _database = bucket;
        _retentionPeriod = retentionPeriod;
        _logger = logger;
    }

    public async Task EnsureDatabaseStructureAsync()
    {
        try
        {
            using var client = new LegacyInfluxClient(_endpointUrl, _authToken);
            var orgApi = client.GetOrganizationsApi();
            var bucketApi = client.GetBucketsApi();

            var organizations = await orgApi.FindOrganizationsAsync(org: _organization);
            var org = organizations.FirstOrDefault(o => o.Name == _organization);
            if (org == null)
            {
                _logger?.LogInformation("Creating organization {Org}", _organization);
                org = await orgApi.CreateOrganizationAsync(_organization);
            }

            var bucket = await bucketApi.FindBucketByNameAsync(_bucket);
            if (bucket == null)
            {
                _logger?.LogInformation(
                    "Creating bucket {Bucket} with retention {Retention}",
                    _bucket,
                    _retentionPeriod);

                var retentionRule = new BucketRetentionRules(BucketRetentionRules.TypeEnum.Expire,
                    (long)_retentionPeriod.TotalSeconds);
                await bucketApi.CreateBucketAsync(_bucket, retentionRule, org!);
            }

            // Create tables for the measurements if they do not exist
            using var sqlClient = new InfluxDb3Client(
                _endpointUrl,
                token: _authToken,
                database: _database);

            var createStatements = new[]
            {
                @"CREATE TABLE IF NOT EXISTS inverter_data (
                    time TIMESTAMPTZ NOT NULL,
                    BuildingId TAG,
                    InverterId TAG,
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
                    BuildingId TAG,
                    BatteryId TAG,
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
                    BuildingId TAG,
                    MeterId TAG,
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
                    BuildingId TAG,
                    DeviceId TAG,
                    firmware_version STRING,
                    comm_status BOOLEAN,
                    last_update_time TIMESTAMPTZ)"
            };

            foreach (var sql in createStatements)
            {
                await foreach (var _ in sqlClient.Query(sql, QueryType.SQL, _database)) { }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to ensure InfluxDB database structure");
            throw;
        }
    }
}
