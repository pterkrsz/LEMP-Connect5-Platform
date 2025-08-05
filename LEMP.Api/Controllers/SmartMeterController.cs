using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InfluxDB3.Client;
using LEMP.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace LEMP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SmartMeterController : ControllerBase
    {
        private readonly InfluxDBClient _client;

        public SmartMeterController(InfluxDBClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Queries recent smart meter measurements from InfluxDB.
        /// </summary>
        /// <param name="limit">Maximum number of rows to return.</param>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Get([FromQuery] int limit = 10)
        {
            if (limit <= 0)
            {
                limit = 1;
            }

            var sql = $"select node, voltage, current, activePower, apparentPower, reactivePower, powerFactor, frequency, importedActiveEnergy, exportedActiveEnergy, importedReactiveEnergy, exportedReactiveEnergy, totalActiveEnergy, time from smartmeter order by time desc limit {limit}";
            var result = new List<SmartMeterDto>();

            await foreach (var row in _client.Query(query: sql))
            {
                if (row.Length < 14)
                {
                    continue;
                }

                result.Add(new SmartMeterDto
                {
                    Node = row[0]?.ToString(),
                    Voltage = Convert.ToDouble(row[1] ?? 0d),
                    Current = Convert.ToDouble(row[2] ?? 0d),
                    ActivePower = Convert.ToDouble(row[3] ?? 0d),
                    ApparentPower = Convert.ToDouble(row[4] ?? 0d),
                    ReactivePower = Convert.ToDouble(row[5] ?? 0d),
                    PowerFactor = Convert.ToDouble(row[6] ?? 0d),
                    Frequency = Convert.ToDouble(row[7] ?? 0d),
                    ImportedActiveEnergy = Convert.ToDouble(row[8] ?? 0d),
                    ExportedActiveEnergy = Convert.ToDouble(row[9] ?? 0d),
                    ImportedReactiveEnergy = Convert.ToDouble(row[10] ?? 0d),
                    ExportedReactiveEnergy = Convert.ToDouble(row[11] ?? 0d),
                    TotalActiveEnergy = Convert.ToDouble(row[12] ?? 0d),
                    Timestamp = row[13] is DateTime dt ? dt : DateTime.UtcNow
                });
            }

            return Ok(result);
        }
    }
}
