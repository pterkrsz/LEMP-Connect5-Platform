using LEMP.Infrastructure.Data;
using LEMP.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace LEMP.Test;

public class EfTwoFactorServiceTests
{
    private static MeasurementDbContext CreateContext()
    {
        var conn = Environment.GetEnvironmentVariable("TEST_DB_CONN") ??
                   "Host=localhost;Port=5432;Database=testdb;Username=testuser;Password=testpass";
        var options = new DbContextOptionsBuilder<MeasurementDbContext>()
            .UseNpgsql(conn)
            .Options;
        var ctx = new MeasurementDbContext(options);
        EnsureSchema(ctx);
        ctx.Database.ExecuteSqlRaw("TRUNCATE TABLE \"Measurements\" RESTART IDENTITY; TRUNCATE TABLE \"TwoFactorSecrets\" RESTART IDENTITY;");
        return ctx;
    }

    private static void EnsureSchema(MeasurementDbContext context)
    {
        const string sql = @"CREATE TABLE IF NOT EXISTS ""Measurements"" (
                                ""Id"" SERIAL PRIMARY KEY,
                                ""SourceType"" TEXT NOT NULL,
                                ""SourceId"" TEXT NOT NULL,
                                ""Timestamp"" TIMESTAMPTZ NOT NULL,
                                ""Values"" TEXT NOT NULL);
                            CREATE TABLE IF NOT EXISTS ""TwoFactorSecrets"" (
                                ""Id"" SERIAL PRIMARY KEY,
                                ""Username"" TEXT NOT NULL,
                                ""EncryptedSecret"" TEXT NOT NULL);";
        context.Database.ExecuteSqlRaw(sql);
    }

    private static IConfiguration CreateConfig()
    {
        var dict = new Dictionary<string, string> { { "EncryptionKey", "my-test-key" } };
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    [Test]
    public async Task SetAndGetSecretAsync()
    {
        await using var context = CreateContext();
        var service = new EfTwoFactorService(context, CreateConfig());

        await service.SetSecretAsync("user1", "SECRET");

        var entry = await context.TwoFactorSecrets.SingleAsync(e => e.Username == "user1");
        Assert.That(entry.EncryptedSecret, Is.Not.EqualTo("SECRET"));

        var retrieved = await service.GetSecretAsync("user1");
        Assert.That(retrieved, Is.EqualTo("SECRET"));
    }

    [Test]
    public async Task GetSecretReturnsNullForUnknownUser()
    {
        await using var context = CreateContext();
        var service = new EfTwoFactorService(context, CreateConfig());

        var result = await service.GetSecretAsync("unknown");
        Assert.That(result, Is.Null);
    }
}
