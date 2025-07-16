// Program.cs
using LEMP.Application.Interfaces;
using LEMP.Infrastructure.Services;
using InfluxDB3.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// 1) Configuration betöltése
var influxSection = builder.Configuration.GetSection("InfluxDB");
var influxHost = influxSection["Host"] ?? "localhost";
var influxPort = influxSection["Port"] ?? "8181";
var influxToken = influxSection["Token"] ?? "";
var influxBucket = influxSection["Bucket"] ?? "";
var influxOrg = influxSection["Org"] ?? "";

// 2) InfluxDBClient regisztrálása (Flight SQL használatra)
builder.Services.AddSingleton(_ =>
    new InfluxDBClient(
        $"http://{influxHost}:{influxPort}",
        token: influxToken,
        database: influxBucket
    )
);

// 3) Initializer regisztrálása
builder.Services.AddSingleton<InfluxDbInitializer>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<InfluxDbInitializer>>();
    return new InfluxDbInitializer(
        endpoint: $"http://{influxHost}:{influxPort}",
        authToken: influxToken,
        organization: influxOrg,
        bucket: influxBucket,
        retentionDays: 30,
        logger: logger
    );
});

// 4) DataPointService regisztrálása
builder.Services.AddScoped<IDataPointService>(sp =>
{
    var client = sp.GetRequiredService<InfluxDBClient>();
    var log = sp.GetRequiredService<ILogger<InfluxDataPointService>>();
    return new InfluxDataPointService(client, log);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 5) Health-check
using var http = new HttpClient { BaseAddress = new Uri($"http://{influxHost}:{influxPort}") };
http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", influxToken);
var health = await http.GetAsync("/health");
health.EnsureSuccessStatusCode();
Console.WriteLine($"[Init] InfluxDB health OK: {await health.Content.ReadAsStringAsync()}");

// 6) Inicializálás Flight SQL-lel
using (var scope = app.Services.CreateScope())
{
    var init = scope.ServiceProvider.GetRequiredService<InfluxDbInitializer>();
    await init.EnsureDatabaseStructureAsync();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.MapControllers();
app.Run();
