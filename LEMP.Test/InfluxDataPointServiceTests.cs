using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using LEMP.Domain.DataPoints;
using LEMP.Infrastructure.Services;
using LEMP.Application.Interfaces;
using NUnit.Framework;

namespace LEMP.Test;

public class InfluxDataPointServiceTests
{
    private class FakeWriteApi : IWriteApiAsync
    {
        public List<object> Written { get; } = new();

        public Task WriteMeasurementAsync<T>(T measurement, WritePrecision precision, string bucket, string org, CancellationToken cancellationToken = default)
        {
            Written.Add(measurement!);
            return Task.CompletedTask;
        }

        // Unused interface methods
        public Task WriteRecordAsync(string record, WritePrecision precision, string bucket, string org, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task WriteRecordsAsync(List<string> records, WritePrecision precision, string bucket, string org, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task WriteRecordsAsync(string[] records, WritePrecision precision, string bucket, string org, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task WriteRecordsAsyncWithIRestResponse(IEnumerable<string> records, WritePrecision precision, string bucket, string org, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task WritePointAsync(PointData point, string bucket, string org, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task WritePointsAsync(List<PointData> points, string bucket, string org, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task WritePointsAsync(PointData[] points, string bucket, string org, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task WritePointsAsyncWithIRestResponse(IEnumerable<PointData> points, string bucket, string org, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task WriteMeasurementsAsync<T>(List<T> measurements, WritePrecision precision, string bucket, string org, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class FakeClient : InfluxDBClient
    {
        private readonly FakeWriteApi _writeApi;
        public FakeClient(FakeWriteApi writeApi) : base("http://localhost", "token")
        {
            _writeApi = writeApi;
        }

        public override IWriteApiAsync GetWriteApiAsync() => _writeApi;
    }

    [Test]
    public async Task WriteAsyncStoresMeasurement()
    {
        var fakeApi = new FakeWriteApi();
        var client = new FakeClient(fakeApi);
        var service = new InfluxDataPointService(client, "bucket", "org");

        var point = new InverterDataPoint { BuildingId = "b1", InverterId = "i1", PowerActive = 1, Timestamp = DateTime.UtcNow };

        await service.WriteAsync(point);

        Assert.That(fakeApi.Written.Count, Is.EqualTo(1));
        Assert.That(fakeApi.Written[0], Is.SameAs(point));
    }
}
