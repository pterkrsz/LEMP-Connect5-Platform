using LEMP.Application.Constants;
using LEMP.Application.DTOs;
using LEMP.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LEMP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MeasurementController : ControllerBase
{
    private readonly IMeasurementService _service;

    public MeasurementController(IMeasurementService service)
    {
        _service = service;
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin + "," + Roles.Operator)]
    public async Task<IActionResult> Post([FromBody] MeasurementDto dto)
    {
        await _service.AddMeasurementAsync(dto);
        return Ok();
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin + "," + Roles.Operator + "," + Roles.ReadOnly)]
    public async Task<ActionResult<IEnumerable<MeasurementDto>>> Get()
    {
        var all = await _service.GetAllAsync();
        return Ok(all);
    }
}
