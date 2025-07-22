using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LEMP.Infrastructure.Services
{

    // Executes raw HTTP calls against the InfluxDB API for testing purposes

    public class InfluxRawTestService
    {
        private readonly IHttpClientFactory _factory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<InfluxRawTestService> _logger;

        // Service dependencies are injected via constructor

        public InfluxRawTestService(IHttpClientFactory factory, IConfiguration configuration, ILogger<InfluxRawTestService> logger)
        {
            _factory = factory;
            _configuration = configuration;
            _logger = logger;
        }


        // Executes all raw HTTP tests
        public async Task RunAsync()
        {
            var client = _factory.CreateClient("Influx");
            var bucket = _configuration["InfluxDB:Bucket"] ?? string.Empty;
            var org = _configuration["InfluxDB:Org"] ?? string.Empty;

            await WriteLineProtocolAsync(client, bucket, org);
            await QuerySqlAsync(client, bucket);
            await QueryInfluxQlAsync(client, bucket);
            await CheckHealthAsync(client);
            await ConfigureDatabaseAsync(client, bucket, org);
            await ConfigureTableAsync(client, bucket);
            await InstallPackagesAsync(client);
            await ListTokensAsync(client);
        }

        private async Task WriteLineProtocolAsync(HttpClient client, string bucket, string org)
        {
            var url = $"/api/v3/write_lp?db={Uri.EscapeDataString(bucket)}&org={Uri.EscapeDataString(org)}&precision=ns";
            await SendAsync(client, HttpMethod.Post, url, "test,tag=example value=123", "text/plain");
        }

        private async Task QuerySqlAsync(HttpClient client, string bucket)
        {
            var body = JsonSerializer.Serialize(new { db = bucket, query = "SELECT * FROM test LIMIT 1", format = "json" });
            await SendAsync(client, HttpMethod.Post, "/api/v3/query/sql", body);
        }

        private async Task QueryInfluxQlAsync(HttpClient client, string bucket)
        {
            var body = JsonSerializer.Serialize(new { db = bucket, query = "SELECT * FROM test LIMIT 1", format = "json" });
            await SendAsync(client, HttpMethod.Post, "/api/v3/query/influxql", body);
        }

        private async Task CheckHealthAsync(HttpClient client)
        {
            await SendAsync(client, HttpMethod.Get, "/health");
            await SendAsync(client, HttpMethod.Get, "/ping");
            await SendAsync(client, HttpMethod.Get, "/metrics");
        }

        private async Task ConfigureDatabaseAsync(HttpClient client, string bucket, string org)
        {
            await SendAsync(client, HttpMethod.Get, $"/api/v3/configure/database?db={Uri.EscapeDataString(bucket)}");
            var body = JsonSerializer.Serialize(new { db = bucket, org });
            await SendAsync(client, HttpMethod.Post, "/api/v3/configure/database", body);
            await SendAsync(client, HttpMethod.Delete, $"/api/v3/configure/database?db={Uri.EscapeDataString(bucket)}");
        }

        private async Task ConfigureTableAsync(HttpClient client, string bucket)
        {
            var body = JsonSerializer.Serialize(new
            {
                db = bucket,
                table = "test_table",
                columns = new[]
                {
                    new { name = "_time", type = "timestamp" },
                    new { name = "value", type = "double" }
                }
            });
            await SendAsync(client, HttpMethod.Post, "/api/v3/configure/table", body);
            await SendAsync(client, HttpMethod.Delete, $"/api/v3/configure/table?db={Uri.EscapeDataString(bucket)}&table=test_table");
        }

        private async Task InstallPackagesAsync(HttpClient client)
        {
            var body = JsonSerializer.Serialize(new { packages = new[] { "influxdb3-python" } });
            await SendAsync(client, HttpMethod.Post, "/api/v3/configure/plugin_environment/install_packages", body);
        }

        private async Task ListTokensAsync(HttpClient client)
        {
            var body = JsonSerializer.Serialize(new { db = "_internal", query = "SELECT id, name, permissions FROM system.tokens", format = "json" });
            await SendAsync(client, HttpMethod.Post, "/api/v3/query/sql", body);
        }

        private async Task SendAsync(HttpClient client, HttpMethod method, string path, string? body = null, string media = "application/json")
        {
            HttpResponseMessage res;
            if (method == HttpMethod.Get)
            {
                res = await client.GetAsync(path);
            }
            else
            {
                var req = new HttpRequestMessage(method, path);
                if (body != null)
                {
                    req.Content = new StringContent(body, Encoding.UTF8, media);
                }
                res = await client.SendAsync(req);
            }

            _logger.LogInformation("[Test] {Method} {Path} -> {Status}", method.Method, path, res.StatusCode);
        }
    }
}
