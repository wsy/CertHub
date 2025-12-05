using System.Text;

using Microsoft.Extensions.Logging;

using Renci.SshNet;

namespace WSY.CertHub.Targets.SSH;

public class SshTarget : ITarget
{
    private const string DefaultDeployPath = "/opt/docker/nginx/";

    public string Name { get; }
    private readonly IConfigurationSection configSection;
    private readonly ILogger<SshTarget> logger;

    public SshTarget([ServiceKey] string serviceKey, IConfiguration configuration, ILogger<SshTarget> logger)
    {
        this.logger = logger;
        ValidateServiceKey(serviceKey);
        Name = serviceKey;
        configSection = configuration.GetRequiredSection(serviceKey);
        logger.LogInformation("Service initialized: {Name}", Name);
    }

    internal static void ValidateServiceKey(string serviceKey)
    {
        if (!serviceKey.StartsWith("Targets:SSH:"))
        {
            throw new ArgumentException("Invalid service key for SshTarget: " + serviceKey);
        }
    }

    public async Task DeployCertificateAsync(string domainName, byte[] publicKey, byte[] privateKey, string? password = null, CancellationToken cancellationToken = default)
    {
        await UploadCertificateAsync(domainName, publicKey, privateKey, cancellationToken);

        string? postDeployCommand = configSection["PostDeployCommand"];
        if (!string.IsNullOrWhiteSpace(postDeployCommand))
        {
            using var sshClient = GetSshClient();
            await sshClient.ConnectAsync(cancellationToken);
            using var command = sshClient.RunCommand(postDeployCommand);
            await command.ExecuteAsync(cancellationToken);
            if (logger.IsEnabled(LogLevel.Information))
            {
                using StreamReader streamReader = new(command.ExtendedOutputStream, Encoding.UTF8);
                logger.LogInformation("PostDeployCommand Error: {Error}", command.Error);
                logger.LogInformation("PostDeployCommand Result: {Result}", command.Result);
                logger.LogInformation("PostDeployCommand Output: {Output}", await streamReader.ReadToEndAsync(cancellationToken));
            }
        }
    }

    private async Task UploadCertificateAsync(string domainName, byte[] publicKey, byte[] privateKey, CancellationToken cancellationToken)
    {
        string deployPath = configSection["DeployPath"] ?? DefaultDeployPath;
        using var scpClient = GetScpClient();
        await scpClient.ConnectAsync(cancellationToken);
        using MemoryStream publicKeyStream = new(publicKey);
        using MemoryStream privateKeyStream = new(privateKey);
        scpClient.Upload(publicKeyStream, Path.Combine(deployPath, domainName + ".crt"));
        scpClient.Upload(privateKeyStream, Path.Combine(deployPath, domainName + ".key"));
    }

    private ScpClient GetScpClient()
    {
        string host = configSection["Host"] ?? throw new InvalidOperationException("Missing configuration: SSH Host not configured!");
        if (!int.TryParse(configSection["Port"], out int port))
        {
            port = 22;
        }
        string username = configSection["User"] ?? "root";
        string? password = configSection["Password"];
        string[]? identityFiles = configSection.GetRequiredSection("IdentityFiles").Get<string[]>();
        IPrivateKeySource[]? privateKeyFiles = null;
        if (identityFiles != null)
        {
            privateKeyFiles = new IPrivateKeySource[identityFiles.Length];
            for(int i = 0; i < identityFiles.Length; i++)
            {
                privateKeyFiles[i] = new PrivateKeyFile(identityFiles[i]);
            }
        }
        if (privateKeyFiles != null && privateKeyFiles.Length > 0)
        {
            return new(host, port, username, privateKeyFiles);
        }
        else if (!string.IsNullOrEmpty(password))
        {
            return new(host, port, username, password);
        }
        return new(host, port, username);
    }

    private SshClient GetSshClient()
    {
        string host = configSection["Host"] ?? throw new InvalidOperationException("Missing configuration: SSH Host not configured!");
        if (!int.TryParse(configSection["Port"], out int port))
        {
            port = 22;
        }
        string username = configSection["User"] ?? "root";
        string? password = configSection["Password"];
        string[]? identityFiles = configSection.GetRequiredSection("IdentityFiles").Get<string[]>();
        IPrivateKeySource[]? privateKeyFiles = null;
        if (identityFiles != null)
        {
            privateKeyFiles = new IPrivateKeySource[identityFiles.Length];
            for (int i = 0; i < identityFiles.Length; i++)
            {
                privateKeyFiles[i] = new PrivateKeyFile(identityFiles[i]);
            }
        }
        if (privateKeyFiles != null && privateKeyFiles.Length > 0)
        {
            return new(host, port, username, privateKeyFiles);
        }
        else if (!string.IsNullOrEmpty(password))
        {
            return new(host, port, username, password);
        }
        return new(host, port, username);
    }
}
