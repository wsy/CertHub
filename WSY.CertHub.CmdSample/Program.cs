using System.Security.Cryptography.X509Certificates;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using WSY.CertHub.Core;

namespace WSY.CertHub.CmdSample;

internal class Program
{
    static ILogger<Program> logger = null!;
    static async Task Main(string[] args)
    {
        // TODO CancellationToken support
        CancellationToken cancellationToken = CancellationToken.None;
        Console.WriteLine("Hello, World!");
        Console.WriteLine("");
        var builder = Host.CreateDefaultBuilder(args);

        builder.ConfigureServices(services =>
        {
            services.AddTencentCloudCertificateProvider("CertProviders:TencentCloud:ExampleCloudAccount1", "CertProviders:TencentCloud:ExampleCloudAccount2");
            services.AddSshTargets("Targets:SSH:ExampleSshHost1", "Targets:SSH:ExampleSshHost2", "Targets:SSH:ExampleSshHost3", "Targets:SSH:ExampleSshHost4");
            services.AddSoftEtherTargets("Targets:SoftEther:SoftEtherHost1", "Targets:SoftEther:SoftEtherHost2");
        });
        var app = builder.Build();
        app.Start();

        var services = app.Services;

        logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Logger is working.");

        var tencentAccount1 = services.GetRequiredKeyedService<ICertificateProvider>("CertProviders:TencentCloud:ExampleCloudAccount1");
        var tencentAccount2 = services.GetRequiredKeyedService<ICertificateProvider>("CertProviders:TencentCloud:ExampleCloudAccount2");

        var sshTarget1 = services.GetRequiredKeyedService<ITarget>("Targets:SSH:ExampleSshHost1");
        var sshTarget2 = services.GetRequiredKeyedService<ITarget>("Targets:SSH:ExampleSshHost2");
        var sshTarget3 = services.GetRequiredKeyedService<ITarget>("Targets:SSH:ExampleSshHost3");
        var sshTarget4 = services.GetRequiredKeyedService<ITarget>("Targets:SSH:ExampleSshHost4");
        var softEtherTarget1 = services.GetRequiredKeyedService<ITarget>("Targets:SoftEther:SoftEtherHost1");
        var softEtherTarget2 = services.GetRequiredKeyedService<ITarget>("Targets:SoftEther:SoftEtherHost2");
        await ProcessOneDomainSshNginx(tencentAccount1, "Cts-Office", "example2.office.example.com", sshTarget2);
        await ProcessOneDomainSshNginx(tencentAccount1, "Cts-Service1", "service1.office.example.com", sshTarget2);
        await ProcessOneDomainSshNginx(tencentAccount1, "Cts-Service2", "service2.office.example.com", sshTarget2);
        await ProcessOneDomainSshNginx(tencentAccount1, "Cts-Cloud1", "example1.cloud.example.com", sshTarget1);
        await ProcessOneDomainSshNginx(tencentAccount2, "Home-Cloud", "cloud1.home.example.com", sshTarget3);
        await ProcessOneDomainSshNginx(tencentAccount2, "Home-Lab", "lab.home.example.com", sshTarget4);
        await ProcessOneDomainSshNginx(tencentAccount2, "Home-Service", "service.home.example.com", sshTarget4);
        await ProcessOneDomainSoftEther(tencentAccount1, "Cts-SoftEther", "se-server1.example.com", softEtherTarget1);
        await ProcessOneDomainSoftEther(tencentAccount2, "Home-SoftEther", "se-server2.example.com", softEtherTarget2);
        app.StopAsync().Wait();
    }

    private static async Task ProcessOneDomainSshNginx(ICertificateProvider certProvider, string DomainAlias, string DomainName, ITarget sshTarget)
    {
        var cancellationToken = CancellationToken.None;

        string certId = await ApplyCertificateFromProvider(certProvider, DomainAlias, DomainName, cancellationToken);
        var certificate = await certProvider.DownloadCertificateCrtKeyFormatAsync(certId, cancellationToken);

        await sshTarget.DeployCertificateAsync(DomainName, certificate.Item1, certificate.Item2, cancellationToken: cancellationToken);
        logger.LogInformation("Certificate {certId}({DomainName}) deployed to target {target}.", certId, DomainName, sshTarget.Name);
    }

    private static async Task ProcessOneDomainSoftEther(ICertificateProvider certProvider, string DomainAlias, string DomainName, ITarget sshTarget)
    {
        var cancellationToken = CancellationToken.None;

        string certId = await ApplyCertificateFromProvider(certProvider, DomainAlias, DomainName, cancellationToken);
        var pfxCertificate = await certProvider.DownloadCertificatePfxFormatAsync(certId, cancellationToken);

        var x509Certificate = X509CertificateLoader.LoadPkcs12(pfxCertificate.Item1, pfxCertificate.Item2, X509KeyStorageFlags.Exportable);
        var publicKeyBytes = x509Certificate.Export(X509ContentType.Cert);
        var privateKeyBytes = x509Certificate.GetRSAPrivateKey()?.ExportRSAPrivateKey() ?? throw new InvalidOperationException("Failed to get private key!");

        await sshTarget.DeployCertificateAsync(DomainName, publicKeyBytes, privateKeyBytes, cancellationToken: cancellationToken);
        logger.LogInformation("Certificate {certId}({DomainName}) deployed to target {target}.", certId, DomainName, sshTarget.Name);
    }

    private static async Task<string> ApplyCertificateFromProvider(ICertificateProvider certProvider, string DomainAlias, string DomainName, CancellationToken cancellationToken)
    {
        logger.LogInformation("Begin processing cert for {DomainName} via {Provider}", DomainName, certProvider.Name);
        var certId = await certProvider.RequestCertificateAsync(DomainName, alias: DomainAlias, cancellationToken: cancellationToken);

        var startTime = DateTime.Now;
        while (!await certProvider.CheckCertificateStatusAsync(certId, cancellationToken))
        {
            logger.LogInformation("Certificate {certId}({DomainName}) is not issued yet. Waiting 11 seconds...", certId, DomainName);
            await Task.Delay(TimeSpan.FromSeconds(11), cancellationToken);
        }
        var endTime = DateTime.Now;
        logger.LogInformation("Certificate {certId}({DomainName}) is issued. TimeTaken: {Time}", certId, DomainName, endTime - startTime);

        return certId;
    }
}
