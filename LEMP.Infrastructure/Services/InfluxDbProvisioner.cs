using System;
using System.Threading.Tasks;
using InfluxDB3.Client;
using InfluxDB3.Client.Query;

namespace LEMP.Infrastructure.Services;

public class InfluxDbProvisioner
{
    private readonly string _endpointUrl;
    private readonly string _token;

    public InfluxDbProvisioner(string endpointUrl, string token)
    {
        _endpointUrl = endpointUrl;
        _token = token;
    }

    public async Task EnsureOrgAndBucketAsync(string orgName, string bucketName, TimeSpan retention)
    {
        try
        {
            using var sqlClient = new InfluxDBClient(_endpointUrl, token: _token);

            var createOrg = $"CREATE ORG IF NOT EXISTS \"{orgName}\"";
            await foreach (var _ in sqlClient.Query(createOrg, QueryType.SQL, bucketName)) { }

            var retentionDays = (int)Math.Ceiling(retention.TotalDays);
            var createBucket = $"CREATE BUCKET IF NOT EXISTS \"{bucketName}\" RETENTION {retentionDays}d";
            await foreach (var _ in sqlClient.Query(createBucket, QueryType.SQL, bucketName)) { }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to provision InfluxDB: {ex}");
        }
    }
}
