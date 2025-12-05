namespace WSY.CertHub.Core;

public interface ICertificateProvider
{
    string Name { get; }
    Task<string> RequestCertificateAsync(string domainName, string? alias = null, string? csrKeyPassword = null, string? oldCert = null, CancellationToken cancellationToken = default);
    Task<bool> CheckCertificateStatusAsync(string certificateId, CancellationToken cancellationToken = default);
    Task<Tuple<byte[], byte[]>> DownloadCertificateCrtKeyFormatAsync(string certificateId, CancellationToken cancellationToken = default);
    Task<Tuple<byte[], string>> DownloadCertificatePfxFormatAsync(string certificateId, CancellationToken cancellationToken = default);
}
