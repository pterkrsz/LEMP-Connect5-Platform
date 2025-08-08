using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using InfluxDB3.Client;
using LEMP.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LEMP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SmartMeterController : ControllerBase
    {
        private readonly InfluxDBClient _client;
        private readonly ILogger<SmartMeterController> _logger;

        public SmartMeterController(InfluxDBClient client, ILogger<SmartMeterController> logger)
        {
            _client = client;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Get([FromQuery] int limit = 10)
        {
            if (limit <= 0)
                limit = 1;

            _logger.LogInformation("Fetching {Limit} smart meter readings", limit);

            var sql = $"""
                SELECT
                  node,
                  voltage,
                  current,
                  "activePower",
                  "apparentPower",
                  "reactivePower",
                  "powerFactor",
                  frequency,
                  "importedActiveEnergy",
                  "exportedActiveEnergy",
                  "importedReactiveEnergy",
                  "exportedReactiveEnergy",
                  "totalActiveEnergy",
                  time
                FROM
                  smartmeter
                ORDER BY
                  time DESC
                LIMIT {limit}
                """;

            var result = new List<SmartMeterDto>();

            await foreach (var row in _client.Query(query: sql))
            {
                try
                {
                    result.Add(new SmartMeterDto
                    {
                        Node = row[0]?.ToString(),
                        Voltage = Convert.ToDouble(row[1] ?? 0d, CultureInfo.InvariantCulture),
                        Current = Convert.ToDouble(row[2] ?? 0d, CultureInfo.InvariantCulture),
                        ActivePower = Convert.ToDouble(row[3] ?? 0d, CultureInfo.InvariantCulture),
                        ApparentPower = Convert.ToDouble(row[4] ?? 0d, CultureInfo.InvariantCulture),
                        ReactivePower = Convert.ToDouble(row[5] ?? 0d, CultureInfo.InvariantCulture),
                        PowerFactor = Convert.ToDouble(row[6] ?? 0d, CultureInfo.InvariantCulture),
                        Frequency = Convert.ToDouble(row[7] ?? 0d, CultureInfo.InvariantCulture),
                        ImportedActiveEnergy = Convert.ToDouble(row[8] ?? 0d, CultureInfo.InvariantCulture),
                        ExportedActiveEnergy = Convert.ToDouble(row[9] ?? 0d, CultureInfo.InvariantCulture),
                        ImportedReactiveEnergy = Convert.ToDouble(row[10] ?? 0d, CultureInfo.InvariantCulture),
                        ExportedReactiveEnergy = Convert.ToDouble(row[11] ?? 0d, CultureInfo.InvariantCulture),
                        TotalActiveEnergy = Convert.ToDouble(row[12] ?? 0d, CultureInfo.InvariantCulture),
                        Timestamp = row[13] is DateTime dt ? dt : DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse smart meter row");
                    continue;
                }
            }

            _logger.LogInformation("Returning {Count} smart meter readings", result.Count);
            return Ok(result);
        }
    }
}

