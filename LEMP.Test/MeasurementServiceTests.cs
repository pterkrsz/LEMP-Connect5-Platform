using LEMP.Application.DTOs;
using LEMP.Infrastructure.Data;
using LEMP.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace LEMP.Test;

public class MeasurementServiceTests
{
    [Test]
    public async Task AddMeasurementsEveryFiveSeconds()
    {
        var options = new DbContextOptionsBuilder<MeasurementDbContext>()
            .UseInMemoryDatabase("five-second")
            .Options;
        await using var context = new MeasurementDbContext(options);
        var service = new EfMeasurementService(context);

        for (int i = 0; i < 2; i++)
        {
            await service.AddMeasurementAsync(new MeasurementDto
            {
                SourceType = "Test",
                SourceId = i.ToString(),
                Timestamp = DateTime.UtcNow,
                Values = new() { ["v"] = i }
            });

            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        var all = await service.GetAllAsync();
        Assert.That(all.Count(), Is.EqualTo(2));
    }
}
