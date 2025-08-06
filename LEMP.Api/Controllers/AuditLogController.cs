using System.Collections.Generic;
using System.Threading.Tasks;
using InfluxDB3.Client;
using InfluxDB3.Client.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LEMP.Api.Controllers
{
    /// <summary>
    /// API endpoint for querying audit logs from InfluxDB.
    /// Accessible only to users with the Admin role.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AuditLogController : ControllerBase
    {
        private readonly InfluxDBClient _client;

        /// <summary>
        /// Initializes a new instance of <see cref="AuditLogController"/>.
        /// </summary>
        /// <param name="client">InfluxDB client used for querying audit logs.</param>
        public AuditLogController(InfluxDBClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Returns the latest audit log entries.
        /// </summary>
        /// <param name="limit">Maximum number of entries to return.</param>
        /// <returns>List of audit log rows.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Get([FromQuery] int limit = 100)
        {
            var sql = $"select * from auditlog order by time desc limit {limit}";
            var rows = new List<object?[]>();

            try
            {
                await foreach (var row in _client.Query(query: sql))
                {
                    rows.Add(row);
                }
            }
            catch (InfluxDBApiException ex)
            {
                return StatusCode((int)ex.StatusCode, ex.Message);
            }

            return Ok(rows);
        }
    }
}

