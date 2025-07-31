using LEMP.Api.Models;
using LEMP.Application.Control;
using LEMP.Domain.Control;
using Microsoft.AspNetCore.Mvc;

namespace LEMP.Api.Controllers;

[ApiController]
[Route("api/control")]
public class ControlController : ControllerBase
{
    private readonly ControlEngine _engine;

    public ControlController(ControlEngine engine)
    {
        _engine = engine;
    }

    // POST /api/control/evaluate
    [HttpPost("evaluate")]
    public ActionResult Evaluate([FromBody] ControlEvaluateRequest request)
    {
        var state = _engine.EvaluateState(request.Battery, request.Inverter, request.SmartMeter);
        return Ok(new { state });
    }

    // GET /api/control/config
    [HttpGet("config")]
    public IActionResult GetConfig()
    {
        return Ok(new
        {
            _engine.ShutoffThresholdSOC,
            _engine.RestartThresholdSOC
        });
    }

    // POST /api/control/reset
    [HttpPost("reset")]
    public IActionResult Reset()
    {
        _engine.RequestBatteryReset();
        return Ok(new { status = "reset_requested" });
    }

    // POST /api/control/apply
    [HttpPost("apply")]
    public IActionResult Apply()
    {
        _engine.ApplyLastState();
        return Ok(new { status = "applied" });
    }

    // GET /api/control/state
    [HttpGet("state")]
    public IActionResult GetState()
    {
        var (state, time) = _engine.GetLastState();
        return Ok(new { state, time });
    }

    // POST /api/control/override
    [HttpPost("override")]
    public IActionResult SetOverride([FromQuery] ControlState state)
    {
        _engine.SetOverride(state);
        return Ok(new { status = "override_set", state });
    }

    // DELETE /api/control/override
    [HttpDelete("override")]
    public IActionResult ClearOverride()
    {
        _engine.ClearOverride();
        return Ok(new { status = "override_cleared" });
    }

    // GET /api/control/logs
    [HttpGet("logs")]
    public IActionResult GetLogs() => Ok(_engine.GetLogs());

    // GET /api/control/metrics
    [HttpGet("metrics")]
    public IActionResult GetMetrics()
    {
        return Ok(new
        {
            battery = _engine.LastBattery,
            inverter = _engine.LastInverter,
            smartMeter = _engine.LastSmartMeter
        });
    }

    // GET /api/control/health
    [HttpGet("health")]
    public IActionResult Health() => Ok("ok");

    // POST /api/control/simulate
    [HttpPost("simulate")]
    public ActionResult Simulate([FromBody] ControlEvaluateRequest request)
    {
        var state = _engine.EvaluateState(request.Battery, request.Inverter, request.SmartMeter);
        return Ok(new { state });
    }
}
