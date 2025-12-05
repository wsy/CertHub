using System.IO.Compression;

using Microsoft.Extensions.DependencyInjection;

using TencentCloud.Ssl.V20191205;
using TencentCloud.Ssl.V20191205.Models;

namespace WSY.CertHub.TencentCloud.Cert;

public class TencentCloudCertificateProvider : TencentCloudBaseProvider<SslClient>, ICertificateProvider
{
    private readonly ILogger<TencentCloudCertificateProvider> logger;
    private readonly IConfigurationSection configSection;
    private readonly IDnsProvider? dnsProvider = null;
    // private readonly Dictionary<string, object> dnsVerificationIdMapping = [];

    public TencentCloudCertificateProvider([ServiceKey] string serviceKey, IConfiguration configuration, IServiceProvider services, ILogger<TencentCloudCertificateProvider> logger) : base(serviceKey, configuration, logger)
    {
        this.logger = logger;
        this.configSection = configuration.GetRequiredSection(serviceKey);
        var dnsProviderName = configSection["DnsProvider"];
        if (dnsProviderName != null)
        {
            dnsProvider = services.GetRequiredKeyedService<IDnsProvider>(dnsProviderName);
            throw new NotImplementedException();
        }
    }

    public async Task<string> RequestCertificateAsync(string domainName, string? alias = null, string? csrKeyPassword = null, string? oldCert = null, CancellationToken cancellationToken = default)
    {
        var existingCertificate = await DescribeCertificateByStatusAsync(domainName, cancellationToken);
        if (existingCertificate != null)
        {
            logger.LogInformation("Cert already exists with ID {Id} and ExpireDate {ExpireDate}. Returning existing cert.", existingCertificate.CertificateId, existingCertificate.CertEndTime);
            return existingCertificate.CertificateId;
        }
        logger.LogInformation("Applying new cert.");
        string dnsAuthMethod = "DNS_AUTO";
        if (dnsProvider != null)
        {
            dnsAuthMethod = "DNS";
            throw new NotImplementedException("DNS Auth Method " + dnsAuthMethod + " is not implemented");
        }
        ApplyCertificateRequest request = new() { DvAuthMethod = dnsAuthMethod, DomainName = domainName, DeleteDnsAutoRecord = true, Alias = alias, OldCertificateId = oldCert, CsrKeyPassword = csrKeyPassword };
        var response = await client.ApplyCertificate(request) ?? throw new InvalidOperationException("Tencent Cloud ApplyCertificate returned null");
        if (dnsAuthMethod == "DNS")
        {
            throw new NotImplementedException();
        }
        return response.CertificateId;
    }

    public async Task<bool> CheckCertificateStatusAsync(string certificateId, CancellationToken cancellationToken = default)
    {
        Certificates certInfo = await DescribeCertificateByCertIdAsync(certificateId, cancellationToken);
        logger.LogInformation("Checking Certificate {certificateId} status: {status}", certificateId, certInfo.Status);
        return certInfo.Status == 1; // 1 means issued
    }

    private async Task<Certificates> DescribeCertificateByCertIdAsync(string certificateId, CancellationToken cancellationToken)
    {
        DescribeCertificatesRequest request = new() { CertIds = [certificateId] };
        cancellationToken.ThrowIfCancellationRequested();
        // TODO Cancellation Token
        var response = await client.DescribeCertificates(request);
        var certInfo = response.Certificates.FirstOrDefault() ?? throw new InvalidOperationException("Tencent Cloud DescribeCertificates returned null CertificateSet!");
        return certInfo;
    }

    private async Task<Certificates?> DescribeCertificateByStatusAsync(string domain, CancellationToken cancellationToken)
    {
        DescribeCertificatesRequest request = new() { CertificateStatus = [1] };
        cancellationToken.ThrowIfCancellationRequested();
        // TODO Cancellation Token
        var response = await client.DescribeCertificates(request);
        var certInfo = response.Certificates.FirstOrDefault(c=>c.Domain == domain);
        return certInfo;
    }

    public async Task<Tuple<byte[], byte[]>> DownloadCertificateCrtKeyFormatAsync(string certificateId, CancellationToken cancellationToken = default)
    {
        var domainName = await GetDomainName(certificateId, cancellationToken);
        byte[] zipData = await DownloadCertificateInternal(certificateId, cancellationToken);

        using MemoryStream zipStream = new(zipData);
        using ZipArchive zip = new(zipStream);
        var privateKeyEntry = zip.GetEntry("Nginx/2_" + domainName + ".key") ?? throw new InvalidOperationException("Invalid cert data from Tencent!");
        var publicKeyEntry = zip.GetEntry("Nginx/1_" + domainName + "_bundle.crt") ?? throw new InvalidOperationException("Invalid cert data from Tencent!");
        using var privateKeyStream = await privateKeyEntry.OpenAsync(cancellationToken);
        using var publicKeyStream = await publicKeyEntry.OpenAsync(cancellationToken);
        logger.LogInformation("Certificate data extracted!");
        return new(await Utility.StreamGetBytes(publicKeyStream), await Utility.StreamGetBytes(privateKeyStream));
    }

    public async Task<Tuple<byte[], string>> DownloadCertificatePfxFormatAsync(string certificateId, CancellationToken cancellationToken = default)
    {
        string domainName = await GetDomainName(certificateId, cancellationToken);
        byte[] zipData = await DownloadCertificateInternal(certificateId, cancellationToken);

        using MemoryStream zipStream = new(zipData);
        using ZipArchive zip = new(zipStream);
        var pfxEntry = zip.GetEntry("IIS/" + domainName + ".pfx") ?? throw new InvalidOperationException("Invalid cert data from Tencent!");
        var passwordFile = zip.GetEntry("IIS/keystorePass.txt") ?? throw new InvalidOperationException("Invalid cert data from Tencent!");
        using var pfxStream = await pfxEntry.OpenAsync(cancellationToken);
        using var passwordStream = await passwordFile.OpenAsync(cancellationToken);
        using StreamReader streamReader = new(passwordStream);
        logger.LogInformation("Certificate data extracted!");
        return new(await Utility.StreamGetBytes(pfxStream), await streamReader.ReadToEndAsync(cancellationToken));
    }

    private async Task<byte[]> DownloadCertificateInternal(string certificateId, CancellationToken cancellationToken)
    {
        DownloadCertificateRequest request = new() { CertificateId = certificateId };
        cancellationToken.ThrowIfCancellationRequested();
        // TODO Cancellation Token
        var response = await client.DownloadCertificate(request);
        var zipData = Convert.FromBase64String(response.Content);
        logger.LogInformation("Certificate downloaded!");
        return zipData;
    }

    private async Task<string> GetDomainName(string certificateId, CancellationToken cancellationToken)
    {
        var certInfo = await DescribeCertificateByCertIdAsync(certificateId, cancellationToken);
        var domainName = certInfo.Domain;
        logger.LogInformation("Certificate {certificateId} has domainname {domainName}.", certificateId, domainName);
        return domainName;
    }

    protected override SslClient InitTencentCloudClient(string secretId, string secretKey, string regionId) => new(new() { SecretId = secretId, SecretKey = secretKey }, regionId);
}
