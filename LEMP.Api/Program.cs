using LEMP.Application.Interfaces;
using LEMP.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using InfluxDB3.Client;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();

var influxHost = Environment.GetEnvironmentVariable("INFLUXDB_HOST") ?? "localhost";
var influxPort = Environment.GetEnvironmentVariable("INFLUXDB_HTTP_PORT") ?? "8181";
var influxToken = Environment.GetEnvironmentVariable("INFLUXDB_TOKEN") ?? string.Empty;
var influxBucket = Environment.GetEnvironmentVariable("INFLUXDB_BUCKET") ?? string.Empty;

builder.Services.AddSingleton<IInfluxDBClient>(_ => new InfluxDBClient($"http://{influxHost}:{influxPort}", token: influxToken, database: influxBucket));
builder.Services.AddScoped<IMeasurementService, InfluxMeasurementService>();
builder.Services.AddScoped<ITwoFactorService, InfluxTwoFactorService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
