using System;
using System.Collections.Generic;
using System.Text;

namespace WSY.CertHub.Core
{
    public interface ITarget
    {
        string Name { get; }
        Task DeployCertificateAsync(string domainName, byte[] publicKey, byte[] privateKey, string? password = null, CancellationToken cancellationToken = default);
    }
}
