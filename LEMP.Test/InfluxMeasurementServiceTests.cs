using System.Numerics;
using InfluxDB3.Client;
using InfluxDB3.Client.Write;
using InfluxDB3.Client.Query;
using Apache.Arrow;

using System.Linq;

using LEMP.Application.DTOs;
using LEMP.Infrastructure.Services;
using NUnit.Framework;

namespace LEMP.Test;

public class InfluxMeasurementServiceTests
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
            foreach (var val in Stored)
            {
                yield return val;
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
    public async Task AddAndRetrieveMeasurements()
    {
        var client = new FakeClient();
        var service = new InfluxMeasurementService(client);

        var dto = new MeasurementDto
        {
            SourceType = "Test",
            SourceId = "1",
            Timestamp = DateTime.UtcNow,
            Values = new() { ["v"] = 1.0 }
        };

        await service.AddMeasurementAsync(dto);
        var all = await service.GetAllAsync();
        Assert.That(all.Single().Values["v"], Is.EqualTo(1.0));
    }


    [Test]
    public async Task GetAllReturnsEmptyWhenNoMeasurements()
    {
        var client = new FakeClient();
        var service = new InfluxMeasurementService(client);

        var all = await service.GetAllAsync();
        Assert.That(all, Is.Empty);
    }

    [Test]
    public async Task AddMultipleMeasurementsAndRetrieveAll()
    {
        var client = new FakeClient();
        var service = new InfluxMeasurementService(client);

        var first = new MeasurementDto
        {
            SourceType = "Test",
            SourceId = "1",
            Timestamp = DateTime.UtcNow,
            Values = new() { ["v"] = 1.0 }
        };

        var second = new MeasurementDto
        {
            SourceType = "Test",
            SourceId = "2",
            Timestamp = DateTime.UtcNow,
            Values = new() { ["v"] = 2.0 }
        };

        await service.AddMeasurementAsync(first);
        await service.AddMeasurementAsync(second);

        var all = (await service.GetAllAsync()).ToList();
        Assert.That(all, Has.Count.EqualTo(2));
        Assert.That(all.Any(m => m.SourceId == "1" && m.Values["v"] == 1.0));
        Assert.That(all.Any(m => m.SourceId == "2" && m.Values["v"] == 2.0));
    }

}
