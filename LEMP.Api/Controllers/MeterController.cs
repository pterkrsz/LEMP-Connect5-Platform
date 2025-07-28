using LEMP.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace LEMP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MeterController : ControllerBase
    {
        // GET /api/meters
        [HttpGet]
        public ActionResult<IEnumerable<MeterDto>> GetTorzs()
        {
            var meters = new List<MeterDto>
            {
                new MeterDto
                {
                    MeterId = "mtr001",
                    Location = "epulet A",
                    Gps = new GpsDto { Lat = 47.5, Lon = 19.05 }
                }
            };
            return Ok(meters);
        }

        // GET /api/meters/{id}/measurements
        [HttpGet("{id}/measurements")]
        public ActionResult<IEnumerable<MeterMeasurementDto>> GetMeasurements(string id)
        {
            var data = new List<MeterMeasurementDto>
            {
                new MeterMeasurementDto
                {
                    Timestamp = DateTime.UtcNow,
                    L1Active = 100.5,
                    L1Reactive = 33.1,
                    L1Current = 12.5
                }
            };
            return Ok(data);
        }
    }
}
