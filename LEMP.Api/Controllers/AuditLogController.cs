using System.Collections.Generic;
using System.Threading.Tasks;
using InfluxDB3.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LEMP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AuditLogController : ControllerBase
{
    private readonly InfluxDBClient _client;
    private readonly ILogger<AuditLogController> _logger;

    public AuditLogController(InfluxDBClient client, ILogger<AuditLogController> logger)
    {
        _client = client;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get([FromQuery] int limit = 100)
    {
        _logger.LogInformation("Fetching latest {Count} audit logs", limit);

        var sql = $"SELECT * FROM auditlog ORDER BY time DESC LIMIT {limit}";
        var rows = new List<object?[]>();

        try
        {
            await foreach (var row in _client.Query(query: sql, database: "local_system"))
            {
                rows.Add(row);
            }
        }
        catch (InfluxDBApiException ex)
        {
            _logger.LogError(ex, "InfluxDBApiException while querying audit logs");
            return StatusCode((int)ex.StatusCode, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while querying audit logs");
            return StatusCode(StatusCodes.Status500InternalServerError, ex.ToString());
        }

        _logger.LogInformation("Returning {Count} audit log entries", rows.Count);
        return Ok(rows);
    }
}