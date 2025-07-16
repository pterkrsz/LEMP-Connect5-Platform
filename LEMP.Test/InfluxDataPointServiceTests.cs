using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using RestSharp;
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
        public Task WriteRecordAsync(string record, WritePrecision precision, string bucket, string org, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task WriteRecordsAsync(List<string> records, WritePrecision precision, string bucket, string org, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task WriteRecordsAsync(string[] records, WritePrecision precision, string bucket, string org, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<RestResponse> WriteRecordsAsyncWithIRestResponse(IEnumerable<string> records, WritePrecision precision, string bucket, string org, CancellationToken cancellationToken = default) => Task.FromResult(new RestResponse());
        public Task WritePointAsync(PointData point, string bucket, string org, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task WritePointsAsync(List<PointData> points, string bucket, string org, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task WritePointsAsync(PointData[] points, string bucket, string org, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<RestResponse[]> WritePointsAsyncWithIRestResponse(IEnumerable<PointData> points, string bucket, string org, CancellationToken cancellationToken = default) => Task.FromResult(Array.Empty<RestResponse>());
        public Task WriteMeasurementsAsync<T>(List<T> measurements, WritePrecision precision, string bucket, string org, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task WriteMeasurementsAsync<T>(T[] measurements, WritePrecision precision, string bucket, string org, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<RestResponse> WriteMeasurementsAsyncWithIRestResponse<TM>(IEnumerable<TM> measurements, WritePrecision precision, string bucket, string org, CancellationToken cancellationToken = default) => Task.FromResult(new RestResponse());
    }


    [Test]
    public async Task WriteAsyncStoresMeasurement()
    {
        var fakeApi = new FakeWriteApi();
        var service = new InfluxDataPointService(fakeApi, "bucket", "org");

        var point = new InverterDataPoint { BuildingId = "b1", InverterId = "i1", PowerActive = 1, Timestamp = DateTime.UtcNow };

        await service.WriteAsync(point);

        Assert.That(fakeApi.Written.Count, Is.EqualTo(1));
        Assert.That(fakeApi.Written[0], Is.SameAs(point));
    }
}
