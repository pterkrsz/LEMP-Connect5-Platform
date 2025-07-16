using InfluxDB3.Client;
using InfluxDB3.Client.Write;
using InfluxDB3.Client.Query;
using Apache.Arrow;
using LEMP.Domain.DataPoints;
using LEMP.Infrastructure.Services;
using LEMP.Application.Interfaces;
using NUnit.Framework;

namespace LEMP.Test;

public class InfluxDataPointServiceTests
{
    private class FakeClient : IInfluxDBClient
    {
        public List<PointData> Written { get; } = new();

        public Task WritePointAsync(PointData point, string? database = null, WritePrecision? precision = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
        {
            Written.Add(point);

            return Task.CompletedTask;
        }

        // Unused interface methods

        public void Dispose() { }
        public IAsyncEnumerable<object?[]> Query(string query, QueryType? queryType = null, string? database = null, Dictionary<string, object>? namedParameters = null, Dictionary<string, string>? headers = null) => throw new NotImplementedException();
        public IAsyncEnumerable<PointDataValues> QueryPoints(string query, QueryType? queryType = null, string? database = null, Dictionary<string, object>? namedParameters = null, Dictionary<string, string>? headers = null) => throw new NotImplementedException();
        public IAsyncEnumerable<RecordBatch> QueryBatches(string query, QueryType? queryType = null, string? database = null, Dictionary<string, object>? namedParameters = null, Dictionary<string, string>? headers = null) => throw new NotImplementedException();
        public Task WriteRecordAsync(string record, string? database = null, WritePrecision? precision = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task WriteRecordsAsync(IEnumerable<string> records, string? database = null, WritePrecision? precision = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task WritePointsAsync(IEnumerable<PointData> points, string? database = null, WritePrecision? precision = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) => Task.CompletedTask;

    }


    [Test]
    public async Task WriteAsyncStoresMeasurement()
    {

        var fakeClient = new FakeClient();
        var service = new InfluxDataPointService(fakeClient);


        var point = new InverterDataPoint { BuildingId = "b1", InverterId = "i1", PowerActive = 1, Timestamp = DateTime.UtcNow };

        await service.WriteAsync(point);


        Assert.That(fakeClient.Written.Count, Is.EqualTo(1));
        Assert.That(fakeClient.Written[0].GetTag("InverterId"), Is.EqualTo("i1"));

    }
}
