using LEMP.Application.Interfaces;
using LEMP.Domain.DataPoints;
using Microsoft.AspNetCore.Mvc;

namespace LEMP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataPointController : ControllerBase
{
    private readonly IDataPointService _service;

    public DataPointController(IDataPointService service)
    {
        _service = service;
    }

    [HttpPost("inverter")]
    public async Task<IActionResult> PostInverter([FromBody] InverterDataPoint point)
    {
        await _service.WriteAsync(point);
        return Ok();
    }

    [HttpPost("bms")]
    public async Task<IActionResult> PostBms([FromBody] BmsDataPoint point)
    {
        await _service.WriteAsync(point);
        return Ok();
    }

    [HttpPost("smartmeter")]
    public async Task<IActionResult> PostSmartMeter([FromBody] SmartMeterDataPoint point)
    {
        await _service.WriteAsync(point);
        return Ok();
    }

    [HttpPost("meta")]
    public async Task<IActionResult> PostMeta([FromBody] MetaDataPoint point)
    {
        await _service.WriteAsync(point);
        return Ok();
    }
}
