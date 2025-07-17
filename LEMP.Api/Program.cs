// NuGet packages:
// dotnet add package InfluxDB3.Client
// dotnet add package Microsoft.Extensions.Http

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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

            // HttpClient DI
            builder.Services.AddHttpClient("Influx", c =>
            {
                c.BaseAddress = baseUri;
                c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            });

            var app = builder.Build();
            Console.WriteLine("Running API tests:");
            await RunTests(app.Services, bucket, org, node);

            Console.WriteLine("Starting web host...");
            app.Run();
        }

        private static async Task RunTests(IServiceProvider sp, string bucket, string org, string node)
        {
            var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient("Influx");

            // 1) Write line protocol to database

            var writeUrl = $"/api/v3/write_lp?db={Uri.EscapeDataString(bucket)}&org={Uri.EscapeDataString(org)}&precision=ns";

            await Test(client, HttpMethod.Post, writeUrl, "test,tag=example value=123", "text/plain");

            // 2) Query SQL via POST
            var sqlBody = JsonSerializer.Serialize(new
            {

                db = bucket,
                query = "SELECT * FROM test LIMIT 1",
                format = "json"

            });
            await Test(client, HttpMethod.Post, "/api/v3/query/sql", sqlBody);

            // 3) Query InfluxQL via POST
            var influxqlBody = JsonSerializer.Serialize(new
            {

                db = bucket,
                query = "SELECT * FROM test LIMIT 1",
                format = "json"

            });
            await Test(client, HttpMethod.Post, "/api/v3/query/influxql", influxqlBody);

            // 4) Health, Ping, Metrics (no auth params)
            await Test(client, HttpMethod.Get, "/health");
            await Test(client, HttpMethod.Get, "/ping");
            await Test(client, HttpMethod.Get, "/metrics");

            // 5) Database configuration

            await Test(client, HttpMethod.Get, "/api/v3/configure/database?db=" + Uri.EscapeDataString(bucket));
            var dbBody = JsonSerializer.Serialize(new { db = bucket, org });
            await Test(client, HttpMethod.Post, "/api/v3/configure/database", dbBody);
            await Test(client, HttpMethod.Delete, "/api/v3/configure/database?db=" + Uri.EscapeDataString(bucket));

            // 6) Table configuration
            var tblBody = JsonSerializer.Serialize(new
            {
                db = bucket,
                table = "test_table",

                columns = new[]
                {
                    new { name = "_time", type = "timestamp" },
                    new { name = "value", type = "double" }
                }

            });
            await Test(client, HttpMethod.Post, "/api/v3/configure/table", tblBody);
            await Test(client, HttpMethod.Delete, "/api/v3/configure/table?db=" + Uri.EscapeDataString(bucket) + "&table=test_table");

            // 7) Plugin environment install packages
            var pkgBody = JsonSerializer.Serialize(new { packages = new[] { "influxdb3-python" } });
            await Test(client, HttpMethod.Post, "/api/v3/configure/plugin_environment/install_packages", pkgBody);

            // 8) List tokens via SQL
            var tokenBody = JsonSerializer.Serialize(new
            {

                db = "_internal",
                query = "SELECT id, name, permissions FROM system.tokens",
                format = "json"

            });
            await Test(client, HttpMethod.Post, "/api/v3/query/sql", tokenBody);
        }

        private static async Task Test(HttpClient client, HttpMethod method, string path, string? body = null, string media = "application/json")
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
                    req.Content = new StringContent(body, Encoding.UTF8, media);
                res = await client.SendAsync(req);
            }
            Console.WriteLine($"[Test] {method.Method} {path} -> {res.StatusCode}");
        }
    }
}
