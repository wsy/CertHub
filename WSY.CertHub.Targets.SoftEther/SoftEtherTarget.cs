namespace WSY.CertHub.Targets.SoftEther;

public class SoftEtherTarget : ITarget
{
    private const int DEFAULT_PORT = 5555;
    public string Name { get; }
    private readonly ILogger<SoftEtherTarget> logger;
    private readonly IConfigurationSection configSection;
    private readonly VpnServerClient vpnServer;

    public SoftEtherTarget([ServiceKey] string serviceKey, IConfiguration configuration, ILogger<SoftEtherTarget> logger)
    {
        this.logger = logger;
        Name = serviceKey;
        configSection = configuration.GetRequiredSection(serviceKey);
        string host = configSection["Host"] ?? throw new InvalidOperationException("Invalid config");
        if (!int.TryParse(configSection["Port"], out int port))
        {
            port = DEFAULT_PORT;
        }
        string password = configSection["Password"] ?? throw new InvalidOperationException("Invalid config");
        vpnServer = new(host, port, password);
        logger.LogInformation("Service initialized: {Name}", Name);
    }

    public async Task DeployCertificateAsync(string domainName, byte[] publicKey, byte[] privateKey, string? password = null, CancellationToken cancellationToken = default)
    {
        await vpnServer.SetServerCertAsync(new() { PrivateKey = privateKey, PublicKey = publicKey }, cancellationToken);
    }

    internal static void ValidateServiceKey(string serviceKey)
    {
        if (!serviceKey.StartsWith("Targets:SoftEther:"))
        {
            throw new ArgumentException("Invalid service key for SoftEtherTarget: " + serviceKey);
        }
    }
}
