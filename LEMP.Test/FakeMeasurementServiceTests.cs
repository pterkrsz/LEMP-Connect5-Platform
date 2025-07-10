using LEMP.Application.DTOs;
using LEMP.Infrastructure.Services;
using NUnit.Framework;

namespace LEMP.Test;

public class FakeMeasurementServiceTests
{
    [Test]
    public async Task AddAndRetrieveMeasurements()
    {
        var service = new FakeMeasurementService();

        var dto = new MeasurementDto
        {
            SourceType = "Test",
            SourceId = "1",
            Timestamp = DateTime.UtcNow,
            Values = new() { ["a"] = 1.2 }
        };

        await service.AddMeasurementAsync(dto);

        var all = await service.GetAllAsync();
        Assert.That(all.Single().SourceId, Is.EqualTo("1"));
    }
}
