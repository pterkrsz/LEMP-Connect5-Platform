using System;
using System.Net.Http.Headers;
using InfluxDB3.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LEMP.Infrastructure.Extensions
{
    /// <summary>
    /// Extension methods for registering InfluxDB related services.
    /// </summary>
    public static class InfluxServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="InfluxDBClient"/> for dependency injection.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="configuration">Application configuration.</param>
        /// <returns>The modified service collection.</returns>
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

        /// <summary>
        /// Registers a raw HTTP <see cref="HttpClient"/> for InfluxDB requests.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="configuration">Application configuration.</param>
        /// <returns>The modified service collection.</returns>
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
