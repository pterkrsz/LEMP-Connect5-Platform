using LEMP.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace LEMP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InstitutionController : ControllerBase
    {
        // Returns all institutions (mock implementation)
        [HttpGet]
        public ActionResult<IEnumerable<InstitutionDto>> GetAll()
        {
            var data = new List<InstitutionDto>
            {
                new InstitutionDto
                {
                    InstitutionId = "ABC123",
                    Name = "Iskola 1",
                    Address = "1234 Budapest, Iskola u. 1.",
                    Gps = new GpsDto { Lat = 47.5, Lon = 19.05 },
                    Contact = new ContactDto
                    {
                        Name = "Kovacs Bela",
                        Phone = "+3612345678",
                        Email = "bela.kovacs@iskola.hu"
                    }
                }
            };
            return Ok(data);
        }
    }
}
