using LEMP.Application.DTOs;
using LEMP.Infrastructure.Data;
using LEMP.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace LEMP.Test;

public class EfMeasurementServiceTests
{
    [Test]
    public async Task AddAndRetrieveMeasurementsFromDb()
    {
        var options = new DbContextOptionsBuilder<MeasurementDbContext>()
            .UseInMemoryDatabase("measurements")
            .Options;

        await using var context = new MeasurementDbContext(options);
        var service = new EfMeasurementService(context);

        await service.AddMeasurementAsync(new MeasurementDto
        {
            SourceType = "Test",
            SourceId = "1",
            Timestamp = DateTime.UtcNow,
            Values = new() { ["v"] = 1 }
        });

        var all = await service.GetAllAsync();
        Assert.AreEqual(1, all.Count());
    }
}
