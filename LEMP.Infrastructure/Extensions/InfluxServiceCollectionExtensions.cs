using System;
using System.Net.Http.Headers;
using InfluxDB3.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LEMP.Infrastructure.Extensions
{
    // Helpers for registering InfluxDB related services
    public static class InfluxServiceCollectionExtensions
    {
        // Registers InfluxDBClient for dependency injection
        public static IServiceCollection AddInfluxDbClient(this IServiceCollection services, IConfiguration configuration)
        {
            var influx = configuration.GetSection("InfluxDB");
            var host = influx["Host"] ?? "localhost";
            var port = int.Parse(influx["Port"] ?? "8181");
            var token = influx["Token"] ?? string.Empty;
            var bucket = influx["Bucket"] ?? string.Empty;

            var url = new UriBuilder("http", host, port).ToString();
            services.AddSingleton(_ => new InfluxDBClient(url, token: token, database: bucket));
            return services;
        }

        // Registers HttpClient for raw InfluxDB requests
        public static IServiceCollection AddInfluxRawHttpClient(this IServiceCollection services, IConfiguration configuration)
        {
            var influx = configuration.GetSection("InfluxDB");
            var host = influx["Host"] ?? "localhost";
            var port = int.Parse(influx["Port"] ?? "8181");
            var token = influx["Token"] ?? string.Empty;

            var baseUri = new UriBuilder("http", host, port).Uri;

            services.AddHttpClient("Influx", c =>
            {
                c.BaseAddress = baseUri;
                c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            });

            return services;
        }
    }
}
