using InfluxDB3.Client.Query;
using Microsoft.Extensions.Logging;
using InfluxDb3Client = InfluxDB3.Client.InfluxDBClient;
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

            var provisioner = new InfluxDbProvisioner(_endpointUrl, _authToken);
            await provisioner.EnsureOrgAndBucketAsync(_organization, _bucket, _retentionPeriod);


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
