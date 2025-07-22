using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using InfluxDB3.Client;
using InfluxDB3.Client.Write;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LEMP.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Starting application...");
            var builder = WebApplication.CreateBuilder(args);

            // Configuration
            var influx = builder.Configuration.GetSection("InfluxDB");
            var host = influx["Host"] ?? "localhost";
            var port = int.Parse(influx["Port"] ?? "8181");
            var token = influx["Token"] ?? string.Empty;
            var bucket = influx["Bucket"] ?? string.Empty;
            var org = influx["Org"] ?? string.Empty;
            var node = influx["NodeId"] ?? string.Empty;
            Console.WriteLine($"Config: host={host}, port={port}, bucket={bucket}, org={org}, node={node}");
            var baseUri = new UriBuilder("http", host, port).Uri;

            // HttpClient DI for raw HTTP tests
            builder.Services.AddHttpClient("InfluxClient", c =>
            {
                c.BaseAddress = baseUri;
                c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            });

            // SDK client registration
            builder.Services.AddSingleton(_ =>
                new InfluxDBClient(
                    baseUri.ToString(),
                    token: token,
                    database: bucket
                )
            );

            var app = builder.Build();
            Console.WriteLine("Running API tests:");

            // Run raw HTTP tests
            var (successes, errors) = await RunTests(app.Services, bucket);

            Console.WriteLine("\nSummary of test results:");
            Console.WriteLine("Successful endpoints:");
            successes.ForEach(s => Console.WriteLine(s));
            Console.WriteLine("\nEndpoints with errors:");
            if (errors.Count == 0)
                Console.WriteLine("None");
            else
                errors.ForEach(e => Console.WriteLine($"{e.path} -> {e.status}"));

            // Example SDK usage
            Console.WriteLine("\nSeeding data via SDK:");
            var influxSdk = app.Services.GetRequiredService<InfluxDBClient>();
            var point = PointData.Measurement("temperature")
                .SetTag("location", node)
                .SetField("value", 55.15)
                .SetTimestamp(DateTime.UtcNow);
            await influxSdk.WritePointAsync(point);
            Console.WriteLine("Point written via SDK.");

            Console.WriteLine("Starting web host...");
            app.Run();
        }

        private static async Task<(List<string> successes, List<(string path, int status)> errors)> RunTests(
            IServiceProvider sp, string bucket)
        {
            var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient("InfluxClient");
            var successes = new List<string>();
            var errors = new List<(string path, int status)>();

            async Task TestAndRecord(HttpMethod method, string path, string? body = null, string media = "application/json")
            {
                HttpResponseMessage res = method == HttpMethod.Get
                    ? await client.GetAsync(path)
                    : await client.SendAsync(new HttpRequestMessage(method, path)
                    {
                        Content = body != null ? new StringContent(body, Encoding.UTF8, media) : null
                    });

                var code = (int)res.StatusCode;
                var result = $"[Test] {method.Method} {path} -> {code}";
                Console.WriteLine(result);

                // Treat 2xx and 409 Conflict as success
                if (res.IsSuccessStatusCode || res.StatusCode == HttpStatusCode.Conflict)
                    successes.Add(result + (res.StatusCode == HttpStatusCode.Conflict ? " (already exists)" : string.Empty));
                else
                    errors.Add((path, code));

                // Log error details for 400 and 500 errors
                if (code == 400 || code == 500)
                {
                    var detail = await res.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error details for {path}: {detail}");
                }
            }

            // 1) Create database and list
            var dbBody = JsonSerializer.Serialize(new { db = bucket });
            await TestAndRecord(HttpMethod.Post, "/api/v3/configure/database", dbBody);
            await TestAndRecord(HttpMethod.Get, "/api/v3/configure/database?format=json");

            // 2) Create table and verify via simple SQL
            var tblBody = JsonSerializer.Serialize(new
            {
                db = bucket,
                table = "test_table",
                tags = new[] { "host" },
                fields = new[] { new { name = "value", type = "utf8" } }
            });
            await TestAndRecord(HttpMethod.Post, "/api/v3/configure/table", tblBody);

            var sqlExists = JsonSerializer.Serialize(new { db = bucket, q = "SELECT * FROM test_table LIMIT 1", format = "jsonl" });
            await TestAndRecord(HttpMethod.Post, "/api/v3/query_sql", sqlExists);

            // 3) Write Line Protocol with clean Unix newlines to avoid trailing \r
            var batchProtocol =
                "measurement,tag=value field=2 1234567900\n" +
                "measurement,tag=value field=3 1234568000";
            await TestAndRecord(
                HttpMethod.Post,
                $"/api/v3/write_lp?db={Uri.EscapeDataString(bucket)}&precision=nanosecond&accept_partial=true",
                batchProtocol,
                "text/plain"
            );

            // 4) Query SQL and InfluxQL using correct table name
            var sqlBody = JsonSerializer.Serialize(new { db = bucket, q = "SELECT * FROM test_table LIMIT 1", format = "jsonl" });
            await TestAndRecord(HttpMethod.Post, "/api/v3/query_sql", sqlBody);
            await TestAndRecord(HttpMethod.Post, "/api/v3/query_influxql", sqlBody);

            // 5) Health, Ping, Metrics
            await TestAndRecord(HttpMethod.Get, "/health");
            await TestAndRecord(HttpMethod.Get, "/ping");
            await TestAndRecord(HttpMethod.Get, "/metrics");

            // 6) Plugin environment install
            var pkgBody = JsonSerializer.Serialize(new { packages = new[] { "influxdb3-python" } });
            await TestAndRecord(HttpMethod.Post, "/api/v3/configure/plugin_environment/install_packages", pkgBody);

            return (successes, errors);
        }
    }
}
