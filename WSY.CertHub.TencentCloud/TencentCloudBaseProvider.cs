namespace WSY.CertHub.TencentCloud;

public abstract class TencentCloudBaseProvider<TTencentClient> where TTencentClient : AbstractClient
{
    public string Name { get; }

    private const string ServiceKeyPrefix = "TencentCloud";

    //protected readonly ILogger<TencentCloudBaseProvider<TTencentClient>> logger;
    private readonly IConfigurationSection configSection;
    protected readonly TTencentClient client;

    public TencentCloudBaseProvider(string serviceKey, IConfiguration configuration, ILogger<TencentCloudBaseProvider<TTencentClient>> logger)
    {
        //this.logger = logger;
        Name = serviceKey;
        string baseServiceKey = GetBaseServiceKey(serviceKey);
        configSection = configuration.GetRequiredSection(baseServiceKey);
        client = InitTencentCloudClient(
            configSection["SecretId"] ?? throw new InvalidOperationException("Missing configuration for SecretId"),
            configSection["SecretKey"] ?? throw new InvalidOperationException("Missing configuration for SecretKey"),
            configSection["RegionId"] ?? string.Empty
        );
        logger.LogInformation("Service initialized: {Name}", Name);
    }

    internal static void ValidateServiceKey(string serviceKey)
    {
        GetBaseServiceKey(serviceKey);
    }
    private static string GetBaseServiceKey(string serviceKey)
    {
        var serviceKeyParts = serviceKey.Split(':');
        if (serviceKeyParts.Length != 3 || serviceKeyParts[1] != ServiceKeyPrefix)
        {
            throw new ArgumentException("Invalid service key format for Tencent Cloud provider: " + serviceKey);
        }
        string baseServiceKey = ServiceKeyPrefix + ':' + serviceKeyParts[2];
        return baseServiceKey;
    }

    protected abstract TTencentClient InitTencentCloudClient(string secretId, string secretKey, string regionId);
}
