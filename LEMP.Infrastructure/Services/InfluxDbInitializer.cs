// InfluxDbInitializer.cs
using InfluxDB3.Client;
using InfluxDB3.Client.Query;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace LEMP.Infrastructure.Services
{
    public class InfluxDbInitializer
    {
        private readonly string _endpoint;
        private readonly string _token;
        private readonly string _org;
        private readonly string _bucket;
        private readonly int _retentionDays;
        private readonly ILogger<InfluxDbInitializer> _logger;

        public InfluxDbInitializer(
            string endpoint,
            string authToken,
            string organization,
            string bucket,
            int retentionDays,
            ILogger<InfluxDbInitializer> logger)
        {
            _endpoint = endpoint.TrimEnd('/');
            _token = authToken;
            _org = organization;
            _bucket = bucket;
            _retentionDays = retentionDays;
            _logger = logger;
        }

        public async Task EnsureDatabaseStructureAsync()
        {
            var client = new InfluxDBClient(_endpoint, token: _token, database: _bucket);

            // 1) Org létrehozása SQL-el
            var createOrg = $"CREATE ORG IF NOT EXISTS \"{_org}\"";
            await ExecuteCreateAsync(client, createOrg, "Organization");

            // 2) Bucket létrehozása SQL-el
            var createBucket = $"CREATE BUCKET IF NOT EXISTS \"{_bucket}\" RETENTION {_retentionDays}d";
            await ExecuteCreateAsync(client, createBucket, "Bucket");

            // 3) Táblák létrehozása SQL-el
            var tables = new Dictionary<string, string>
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

            foreach (var kv in tables)
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
                    // csak végrehajtjuk, nincs belső érték
                }
                _logger.LogInformation("[Init] {Res} létrehozva vagy már létezik.", resourceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Init] Hiba közben: {Res}", resourceName);
                throw;
            }
        }
    }
}
