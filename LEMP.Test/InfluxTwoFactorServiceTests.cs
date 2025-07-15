using InfluxDB3.Client;
using InfluxDB3.Client.Write;
using InfluxDB3.Client.Query;
using Apache.Arrow;
using System.Linq;
using System.Threading.Tasks;
using LEMP.Infrastructure.Services;
using NUnit.Framework;

namespace LEMP.Test;

public class InfluxTwoFactorServiceTests
{
    private class FakeClient : IInfluxDBClient
    {
        public List<PointDataValues> Stored { get; } = new();

        public Task WritePointAsync(PointData point, string? database = null, WritePrecision? precision = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
        {
            var values = PointDataValues.Measurement(point.GetMeasurement());
            foreach (var tag in point.GetTagNames())
            {
                var v = point.GetTag(tag);
                if (v != null) values.SetTag(tag, v);
            }
            foreach (var field in point.GetFieldNames())
            {
                var v = point.GetField(field);
                if (v != null) values.SetField(field, v);
            }
            var ts = point.GetTimestamp();
            if (ts.HasValue) values.SetTimestamp(ts.Value);
            Stored.Add(values);
            return Task.CompletedTask;
        }

        public async IAsyncEnumerable<PointDataValues> QueryPoints(string query, QueryType? queryType = null, string? database = null, Dictionary<string, object>? namedParameters = null, Dictionary<string, string>? headers = null)
        {
            var username = namedParameters?["username"]?.ToString();
            for (int i = Stored.Count - 1; i >= 0; i--)
            {
                var val = Stored[i];
                if (val.GetTag("username") == username)
                {
                    yield return val;
                }
            }
            await Task.CompletedTask;
        }

        public void Dispose() { }
        public IAsyncEnumerable<object?[]> Query(string query, QueryType? queryType = null, string? database = null, Dictionary<string, object>? namedParameters = null, Dictionary<string, string>? headers = null) => throw new NotImplementedException();
        public IAsyncEnumerable<RecordBatch> QueryBatches(string query, QueryType? queryType = null, string? database = null, Dictionary<string, object>? namedParameters = null, Dictionary<string, string>? headers = null) => throw new NotImplementedException();
        public Task WriteRecordAsync(string record, string? database = null, WritePrecision? precision = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task WriteRecordsAsync(IEnumerable<string> records, string? database = null, WritePrecision? precision = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task WritePointsAsync(IEnumerable<PointData> points, string? database = null, WritePrecision? precision = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    [Test]
    public async Task SetAndGetSecret()
    {
        var client = new FakeClient();
        var service = new InfluxTwoFactorService(client);

        await service.SetSecretAsync("user1", "SECRET");
        var secret = await service.GetSecretAsync("user1");

        Assert.That(secret, Is.EqualTo("SECRET"));
    }

    [Test]
    public async Task GetSecretReturnsNullWhenNoneSaved()
    {
        var client = new FakeClient();
        var service = new InfluxTwoFactorService(client);

        var secret = await service.GetSecretAsync("missing");
        Assert.That(secret, Is.Null);
    }

    [Test]
    public async Task SetSecretReplacesExistingSecret()
    {
        var client = new FakeClient();
        var service = new InfluxTwoFactorService(client);

        await service.SetSecretAsync("user1", "OLD");
        await Task.Delay(5);
        await service.SetSecretAsync("user1", "NEW");

        var secret = await service.GetSecretAsync("user1");
        Assert.That(secret, Is.EqualTo("NEW"));
    }
}
