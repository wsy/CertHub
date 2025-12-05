using System.Text.Json.Serialization;

namespace WSY.CertHub.Targets.SoftEther;

/// <summary>
/// Key pair
/// </summary>
public record VpnServerCertificate
{
    /// <summary>
    /// The body of the certificate
    /// </summary>
    [JsonPropertyName("Cert_bin")]
    public required byte[] PublicKey { get; init; }

    /// <summary>
    /// The body of the private key
    /// </summary>
    [JsonPropertyName("Key_bin")]
    public required byte[] PrivateKey { get; init; }
}
