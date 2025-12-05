namespace WSY.CertHub.Core;

public interface IDnsProvider
{
    string Name { get; }
    Task<object> AddVerificationRecordAsync(string domain, string recordName, string recordValue, CancellationToken cancellationToken = default);
    Task RemoveVerificationRecordAsync(string domain, object recordId, CancellationToken cancellationToken = default);
}
