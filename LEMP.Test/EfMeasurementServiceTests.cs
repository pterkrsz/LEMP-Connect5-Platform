using LEMP.Application.DTOs;
using LEMP.Infrastructure.Data;
using LEMP.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace LEMP.Test;

public class EfMeasurementServiceTests
{
    private static DbContextOptions<MeasurementDbContext> CreateOptions()
    {
        var conn = Environment.GetEnvironmentVariable("TEST_DB_CONN") ??
                   "Host=localhost;Port=5432;Database=testdb;Username=testuser;Password=testpass";
        return new DbContextOptionsBuilder<MeasurementDbContext>()
            .UseNpgsql(conn)
            .Options;
    }

    private static async Task EnsureSchemaAsync(MeasurementDbContext context)
    {
        var sql = @"CREATE TABLE IF NOT EXISTS \"Measurements\" (
                        \"Id\" SERIAL PRIMARY KEY,
                        \"SourceType\" TEXT NOT NULL,
                        \"SourceId\" TEXT NOT NULL,
                        \"Timestamp\" TIMESTAMPTZ NOT NULL,
                        \"Values\" TEXT NOT NULL);
                    CREATE TABLE IF NOT EXISTS \"TwoFactorSecrets\" (
                        \"Id\" SERIAL PRIMARY KEY,
                        \"Username\" TEXT NOT NULL,
                        \"EncryptedSecret\" TEXT NOT NULL);";
        await context.Database.ExecuteSqlRawAsync(sql);
    }

    private static async Task ClearDbAsync(MeasurementDbContext context)
    {
        await EnsureSchemaAsync(context);
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Measurements\" RESTART IDENTITY; TRUNCATE TABLE \"TwoFactorSecrets\" RESTART IDENTITY;");
    }

    [Test]
    public async Task AddAndRetrieveMeasurementsFromDb()
    {
        await using var context = new MeasurementDbContext(CreateOptions());
        await ClearDbAsync(context);

        var service = new EfMeasurementService(context);

        await service.AddMeasurementAsync(new MeasurementDto
        {
            SourceType = "Test",
            SourceId = "1",
            Timestamp = DateTime.UtcNow,
            Values = new() { ["v"] = 1 }
        });

        var all = await service.GetAllAsync();
        Assert.That(all.Count(), Is.EqualTo(1));
    }
}
