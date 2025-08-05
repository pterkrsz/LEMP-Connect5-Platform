using LEMP.Infrastructure.Extensions;
using LEMP.Infrastructure.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "LEMP API", Version = "v1" });
});

builder.Services.AddInfluxDbClient(builder.Configuration);
builder.Services.AddInfluxRawHttpClient(builder.Configuration);

// Service hosting raw HTTP tests if needed.
builder.Services.AddTransient<InfluxRawTestService>();
// Background service pushing smart meter measurements to InfluxDB.
builder.Services.AddHostedService<SmartMeterInfluxForwarder>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
