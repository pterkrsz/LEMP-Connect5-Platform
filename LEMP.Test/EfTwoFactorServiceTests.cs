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
        var options = new DbContextOptionsBuilder<MeasurementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MeasurementDbContext(options);
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
