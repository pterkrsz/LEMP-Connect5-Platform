// ------------------- AuditLogForwarder.cs -------------------
using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LEMP.Infrastructure.Services;

public class AuditLogForwarder : BackgroundService
{
    private readonly IHttpClientFactory _factory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuditLogForwarder> _logger;

    public AuditLogForwarder(
        IHttpClientFactory factory,
        IConfiguration configuration,
        ILogger<AuditLogForwarder> logger)
    {
        _factory = factory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Ez a szolgáltatás most NEM küld heartbeat-et, amíg nem szükséges.
        _logger.LogInformation("AuditLogForwarder initialized, but heartbeat disabled.");
        await Task.CompletedTask;
    }
}
