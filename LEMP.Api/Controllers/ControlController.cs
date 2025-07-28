using LEMP.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace LEMP.Api.Controllers
{
    [ApiController]
    [Route("api/control")]
    public class ControlController : ControllerBase
    {
        // POST /api/control/inverter
        [HttpPost("inverter")]
        public IActionResult ControlInverter([FromBody] InverterControlDto dto)
        {
            // In a real implementation commands would be queued/executed
            return Ok(new { status = "accepted" });
        }

        // POST /api/control/digital
        [HttpPost("digital")]
        public IActionResult SetDeviceState([FromBody] DigitalControlDto dto)
        {
            return Ok(new { status = "accepted" });
        }
    }
}
