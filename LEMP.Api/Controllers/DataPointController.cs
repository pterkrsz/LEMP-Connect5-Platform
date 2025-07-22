using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InfluxDB3.Client;
using InfluxDB3.Client.Write;
using LEMP.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace LEMP.Api.Controllers
{
    /// <summary>
    /// API endpoints for reading and writing InfluxDB datapoints.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DataPointController : ControllerBase
    {
        private readonly InfluxDBClient _client;

        /// <summary>
        /// Initializes a new instance of <see cref="DataPointController"/>.
        /// </summary>
        /// <param name="client">InfluxDB client instance.</param>
        public DataPointController(InfluxDBClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Gets the latest datapoints for a measurement.
        /// </summary>
        /// <param name="measurement">Measurement name.</param>
        /// <param name="limit">Number of points to return.</param>
        /// <returns>Collection of rows from InfluxDB.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Get([FromQuery] string measurement, [FromQuery] int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(measurement))
            {
                return BadRequest("measurement query parameter is required");
            }

            var sql = $"select * from {measurement} order by time desc limit {limit}";
            var rows = new List<object?[]>();
            await foreach (var row in _client.Query(query: sql))
            {
                rows.Add(row);
            }

            return Ok(rows);
        }

        /// <summary>
        /// Writes a datapoint to InfluxDB.
        /// </summary>
        /// <param name="dto">Datapoint payload.</param>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Post([FromBody] DataPointDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Measurement))
            {
                return BadRequest("Measurement is required");
            }

            var point = PointData.Measurement(dto.Measurement);
            if (dto.Tags != null)
            {
                foreach (var tag in dto.Tags)
                {
                    point = point.SetTag(tag.Key, tag.Value);
                }
            }

            if (dto.Fields != null)
            {
                foreach (var field in dto.Fields)
                {
                    point = point.SetField(field.Key, field.Value);
                }
            }

            if (dto.Timestamp.HasValue)
            {
                point = point.SetTimestamp(dto.Timestamp.Value);
            }

            try
            {
                await _client.WritePointAsync(point);
            }
            catch (InfluxDBApiException ex)
            {
                return StatusCode((int)ex.StatusCode, ex.Message);
            }

            return CreatedAtAction(nameof(Get), new { measurement = dto.Measurement, limit = 1 }, null);
        }
    }
}

