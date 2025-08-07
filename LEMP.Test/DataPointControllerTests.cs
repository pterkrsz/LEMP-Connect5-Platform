using System.Threading.Tasks;
using LEMP.Api.Controllers;
using LEMP.Api.Models;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;

namespace LEMP.Test;

public class DataPointControllerTests
{
    [Test]
    public async Task Get_ReturnsBadRequest_WhenMeasurementMissing()
    {
        var controller = new DataPointController(null!);

        var result = await controller.Get(measurement: null!);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Post_ReturnsBadRequest_WhenMeasurementMissing()
    {
        var controller = new DataPointController(null!);
        var dto = new DataPointDto { Measurement = "" };

        var result = await controller.Post(dto);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }
}
