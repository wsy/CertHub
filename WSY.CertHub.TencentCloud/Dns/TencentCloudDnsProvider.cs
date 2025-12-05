using TencentCloud.Dnspod.V20210323;
using TencentCloud.Dnspod.V20210323.Models;

namespace WSY.CertHub.TencentCloud.Dns;

public class TencentCloudDnsProvider([ServiceKey] string serviceKey, IConfiguration configuration, ILogger<TencentCloudDnsProvider> logger)
    : TencentCloudBaseProvider<DnspodClient>(serviceKey, configuration, logger), IDnsProvider
{
    public async Task<object> AddVerificationRecordAsync(string domain, string recordName, string recordValue, CancellationToken cancellationToken = default)
    {
        var existingRecord = await GetVerificationRecord(domain, recordName);
        if (existingRecord == null)
        {
            CreateTXTRecordRequest request = new()
            {
                Domain = domain,
                SubDomain = recordName,
                RecordLine = "默认",
                Value = recordValue,
            };
            var response = await client.CreateTXTRecord(request) ?? throw new InvalidOperationException("Tencent Cloud CreateTXTRecord returned null");
            var recordId = response.RecordId ?? throw new InvalidOperationException("Tencent Cloud CreateTXTRecord returned invalid recordId");
            logger.LogInformation("Created new verification record with ID {Id}.", recordId);
            return recordId;
        }
        if (existingRecord.RecordId == null)
        {
            throw new InvalidOperationException("Tencent Cloud existing record has null RecordId");
        }
        if (existingRecord.Value != recordValue)
        {
            ModifyTXTRecordRequest request = new()
            {
                Domain = domain,
                RecordId = existingRecord.RecordId,
                SubDomain = recordName,
                RecordLine = "默认",
                Value = recordValue,
            };
            var response = await client.ModifyTXTRecord(request) ?? throw new InvalidOperationException("Tencent Cloud ModifyTXTRecord returned null");
            logger.LogInformation("Updated existing verification record with ID {Id}.", existingRecord.RecordId);
            return response.RecordId ?? throw new InvalidOperationException("Tencent Cloud CreateTXTRecord returned invalid recordId");
        }
        logger.LogInformation("Verification record already exists with ID {Id}. Returning existing record.", existingRecord.RecordId);
        return existingRecord.RecordId.Value;
    }

    public async Task RemoveVerificationRecordAsync(string domain, object recordId, CancellationToken cancellationToken = default)
    {
        if (recordId is not ulong rid)
        {
            throw new ArgumentException("recordId must be of type ulong", nameof(recordId));
        }
        DeleteRecordRequest request = new()
        {
            Domain = domain,
            RecordId = rid,
        };
        var _ = await client.DeleteRecord(request) ?? throw new InvalidOperationException("Tencent Cloud CreateTXTRecord returned null");
    }

    private async Task<RecordListItem?> GetVerificationRecord(string domain, string recordName)
    {
        DescribeRecordFilterListRequest request = new()
        {
            Domain = domain,
            SubDomain = recordName,
            RecordType = ["TXT"],
            IsExactSubDomain = true,
        };
        var response = await client.DescribeRecordFilterList(request) ?? throw new InvalidOperationException("Tencent Cloud DescribeRecordFilterList returned null");
        return response.RecordList.FirstOrDefault();
    }

    protected override DnspodClient InitTencentCloudClient(string secretId, string secretKey, string regionId) => new(new() { SecretId = secretId, SecretKey = secretKey }, regionId);
}
