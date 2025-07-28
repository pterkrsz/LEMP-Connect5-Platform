using LEMP.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace LEMP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SensorController : ControllerBase
    {
        // GET /api/sensors
        [HttpGet]
        public ActionResult<IEnumerable<SensorDto>> GetTorzs()
        {
            var sensors = new List<SensorDto>
            {
                new SensorDto
                {
                    DeviceId = "sensor123",
                    SensorType = "temperature",
                    Location = "epulet A, emelet 2"
                }
            };
            return Ok(sensors);
        }

        // GET /api/sensors/{id}/measurements
        [HttpGet("{id}/measurements")]
        public ActionResult<IEnumerable<SensorMeasurementDto>> GetMeasurements(string id)
        {
            var data = new List<SensorMeasurementDto>
            {
                new SensorMeasurementDto
                {
                    DeviceId = id,
                    Temperature = 22.5,
                    Unit = "Celsius",
                    Timestamp = DateTime.UtcNow
                }
            };
            return Ok(data);
        }
    }
}
