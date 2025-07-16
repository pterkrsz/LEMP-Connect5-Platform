using LEMP.Application.Interfaces;
using LEMP.Infrastructure.Services;
using InfluxDB3.Client;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var influxSection = builder.Configuration.GetSection("InfluxDB");
var influxHost = influxSection["Host"] ?? "localhost";
var influxPort = influxSection["Port"] ?? "8181";
var influxToken = influxSection["Token"] ?? string.Empty;
var influxBucket = influxSection["Bucket"] ?? string.Empty;


builder.Services.AddSingleton(_ => new InfluxDBClient($"http://{influxHost}:{influxPort}", token: influxToken, database: influxBucket));

builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<InfluxDbInitializer>>();
    var endpoint = $"http://{influxHost}:{influxPort}";
    var org = influxSection["Org"] ?? string.Empty;
    var retention = TimeSpan.FromDays(30);

    return new InfluxDbInitializer(endpoint, influxToken, org, influxBucket, retention, logger);
});
builder.Services.AddScoped<IDataPointService>(sp =>
{
    var client = sp.GetRequiredService<InfluxDBClient>();
    return new InfluxDataPointService(client, sp.GetRequiredService<ILogger<InfluxDataPointService>>());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<InfluxDbInitializer>();

    await initializer.EnsureDatabaseStructureAsync();

}

app.MapControllers();

app.Run();
