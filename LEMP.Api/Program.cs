using LEMP.Application.Interfaces;
using LEMP.Infrastructure.Services;
using InfluxDB3.Client;
using System.Net.Http;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var influxSection = builder.Configuration.GetSection("InfluxDB");
var influxHost = influxSection["Host"] ?? "localhost";
var influxPort = influxSection["Port"] ?? "8181";
var influxToken = influxSection["Token"] ?? string.Empty;
var influxBucket = influxSection["Bucket"] ?? string.Empty;
var nodeId = influxSection["NodeId"] ?? string.Empty;

// InfluxDB v3 client regisztrálása
builder.Services.AddSingleton(_ =>
    new InfluxDBClient(
        $"http://{influxHost}:{influxPort}",
        token: influxToken,
        database: influxBucket
    )
);

// Initializer regisztrálása
builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<InfluxDBClient>();
    var logger = sp.GetRequiredService<ILogger<InfluxDbInitializer>>();
    var org = influxSection["Org"] ?? string.Empty;
    var retention = TimeSpan.FromDays(30);
    return new InfluxDbInitializer(client, influxBucket, org, retention, logger);
});

// Telemetry service regisztrálása
builder.Services.AddScoped<TelemetryService>();

// DataPoint service regisztrálása
builder.Services.AddScoped<IDataPointService>(sp =>
{
    var client = sp.GetRequiredService<InfluxDBClient>();
    var log = sp.GetRequiredService<ILogger<InfluxDataPointService>>();
    return new InfluxDataPointService(client, log);
});

var app = builder.Build();

// === HEALTH CHECK az InfluxDB-hez ===
using (var healthClient = new HttpClient { BaseAddress = new Uri($"http://{influxHost}:{influxPort}") })
{
    healthClient.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", influxToken);

    var healthResponse = await healthClient.GetAsync("/health");
    if (!healthResponse.IsSuccessStatusCode)
    {
        var body = await healthResponse.Content.ReadAsStringAsync();
        throw new Exception(
            $"InfluxDB health check failed: {(int)healthResponse.StatusCode} {healthResponse.ReasonPhrase}\n{body}"
        );
    }
    // opcionális log:
    var info = await healthResponse.Content.ReadAsStringAsync();
    Console.WriteLine($"InfluxDB health OK: {info}");
}

// Swagger, HTTPS, stb.
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

using var scope = app.Services.CreateScope();
var initializer = scope.ServiceProvider.GetRequiredService<InfluxDbInitializer>();
await initializer.EnsureDatabaseStructureAsync();
// opcionális minta adat
var telemetry = scope.ServiceProvider.GetRequiredService<TelemetryService>();
await telemetry.SendInverterReadingAsync(
    "demo_building",
    "inv_demo",
    1000,
    100,
    50,
    230,
    230,
    230,
    5,
    5,
    5,
    DateTime.UtcNow);

app.MapControllers();
app.Run();
