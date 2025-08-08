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
        _logger.LogWarning("[AuditLogForwarder] Service starting");
        try
        {
            // Ez a szolgáltatás most NEM küld heartbeat-et, amíg nem szükséges.
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AuditLogForwarder] Exception during execution");
            throw;
        }
        finally
        {
            _logger.LogWarning("[AuditLogForwarder] Service stopping");
        }
    }
}
