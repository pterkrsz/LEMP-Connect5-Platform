using InfluxDB3.Client;
using InfluxDB3.Client.Write;
using LEMP.Application.Interfaces;

namespace LEMP.Infrastructure.Services;

public class InfluxTwoFactorService : ITwoFactorService
{
    private readonly IInfluxDBClient _client;

    public InfluxTwoFactorService(IInfluxDBClient client)
    {
        _client = client;
    }

    public async Task<string?> GetSecretAsync(string username)
    {
        const string sql = "SELECT secret FROM two_factor_secrets WHERE username=$username ORDER BY time DESC LIMIT 1";
        await foreach (var point in _client.QueryPoints(sql, namedParameters: new Dictionary<string, object> { ["username"] = username }))
        {
            return point.GetField("secret")?.ToString();
        }
        return null;
    }

    public async Task SetSecretAsync(string username, string secret)
    {
        var point = PointData.Measurement("two_factor_secrets")
            .SetTag("username", username)
            .SetField("secret", secret)
            .SetTimestamp(DateTime.UtcNow);
        await _client.WritePointAsync(point);
    }
}
