using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using Microsoft.Extensions.Logging;

namespace LEMP.Infrastructure.Services;

public class InfluxDbInitializer
{
    private readonly string _endpointUrl;
    private readonly string _authToken;
    private readonly string _organization;
    private readonly string _bucket;
    private readonly TimeSpan _retentionPeriod;
    private readonly ILogger<InfluxDbInitializer>? _logger;

    public InfluxDbInitializer(
        string endpointUrl,
        string authToken,
        string organization,
        string bucket,
        TimeSpan retentionPeriod,
        ILogger<InfluxDbInitializer>? logger = null)
    {
        _endpointUrl = endpointUrl;
        _authToken = authToken;
        _organization = organization;
        _bucket = bucket;
        _retentionPeriod = retentionPeriod;
        _logger = logger;
    }

    public async Task EnsureDatabaseStructureAsync()
    {
        try
        {
            using var client = new InfluxDBClient(_endpointUrl, _authToken);
            var orgApi = client.GetOrganizationsApi();
            var bucketApi = client.GetBucketsApi();

            var organizations = await orgApi.FindOrganizationsAsync(org: _organization);
            var org = organizations.FirstOrDefault(o => o.Name == _organization);
            if (org == null)
            {
                _logger?.LogInformation("Creating organization {Org}", _organization);
                org = await orgApi.CreateOrganizationAsync(_organization);
            }

            var bucket = await bucketApi.FindBucketByNameAsync(_bucket);
            if (bucket == null)
            {
                _logger?.LogInformation(
                    "Creating bucket {Bucket} with retention {Retention}",
                    _bucket,
                    _retentionPeriod);

                var retentionRule = new BucketRetentionRules(BucketRetentionRules.TypeEnum.Expire,
                    (long)_retentionPeriod.TotalSeconds);
                await bucketApi.CreateBucketAsync(_bucket, retentionRule, org!);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to ensure InfluxDB database structure");
            throw;
        }
    }
}
