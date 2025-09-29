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
            var influx = configuration.GetRequiredSection("InfluxDB");
            var host = influx["Host"] ?? throw new InvalidOperationException("InfluxDB:Host is not configured");
            var port = influx.GetValue<int?>("Port")
                       ?? throw new InvalidOperationException("InfluxDB:Port is not configured");
            var token = influx["Token"];
            var bucket = influx["Bucket"]
                        ?? throw new InvalidOperationException("InfluxDB:Bucket is not configured");

            var url = new UriBuilder("http", host, port).ToString();
            services.AddSingleton(_ => new InfluxDBClient(url, token ?? string.Empty, database: bucket));
            return services;
        }


        // Registers HttpClient for raw InfluxDB requests

        public static IServiceCollection AddInfluxRawHttpClient(this IServiceCollection services, IConfiguration configuration)
        {
            var influx = configuration.GetRequiredSection("InfluxDB");
            var host = influx["Host"] ?? throw new InvalidOperationException("InfluxDB:Host is not configured");
            var port = influx.GetValue<int?>("Port")
                       ?? throw new InvalidOperationException("InfluxDB:Port is not configured");
            var token = influx["Token"];

            var baseUri = new UriBuilder("http", host, port).Uri;

            services.AddHttpClient("Influx", c =>
            {
                c.BaseAddress = baseUri;
                if (!string.IsNullOrEmpty(token))
                {
                    c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            });

            return services;
        }
    }
}
