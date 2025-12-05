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
            services.AddTencentCloudCertificateProvider("CertProviders:TencentCloud:Jerry", "CertProviders:TencentCloud:Wsy");
            services.AddSshTargets("Targets:SSH:ITV-WWW", "Targets:SSH:ITV-Docker", "Targets:SSH:WSY-Docker", "Targets:SSH:WSY-AliyunSH", "Targets:SSH:WSY-BwgLA");
            services.AddSoftEtherTargets("Targets:SoftEther:WSY-Shanghai", "Targets:SoftEther:WSY-AliyunSH", "Targets:SoftEther:WSY-BwgLA", "Targets:SoftEther:ITV-Tianjin");
        });
        var app = builder.Build();
        app.Start();

        var services = app.Services;

        logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Logger is working.");

        var tencentJerry = services.GetRequiredKeyedService<ICertificateProvider>("CertProviders:TencentCloud:Jerry");
        var tencentWsy = services.GetRequiredKeyedService<ICertificateProvider>("CertProviders:TencentCloud:Wsy");

        var sshTargetITVWWW = services.GetRequiredKeyedService<ITarget>("Targets:SSH:ITV-WWW");
        var sshTargetITVDocker = services.GetRequiredKeyedService<ITarget>("Targets:SSH:ITV-Docker");
        var sshTargetWSYDocker = services.GetRequiredKeyedService<ITarget>("Targets:SSH:WSY-Docker");
        var sshTargetWSYBwgLa = services.GetRequiredKeyedService<ITarget>("Targets:SSH:WSY-BwgLA");
        var sshTargetWSYAliyunSH = services.GetRequiredKeyedService<ITarget>("Targets:SSH:WSY-AliyunSH");
        var softEtherTargetWSYSH = services.GetRequiredKeyedService<ITarget>("Targets:SoftEther:WSY-Shanghai");
        var softEtherTargetWSYAliyunSH = services.GetRequiredKeyedService<ITarget>("Targets:SoftEther:WSY-AliyunSH");
        var softEtherTargetWSYBwgLA = services.GetRequiredKeyedService<ITarget>("Targets:SoftEther:WSY-BwgLA");
        var softEtherTargetITVTianjin = services.GetRequiredKeyedService<ITarget>("Targets:SoftEther:ITV-Tianjin");
        await ProcessOneDomainSshNginx(tencentJerry, "ITV-Tianjin", "tianjin.itvtech.cn", sshTargetITVDocker);
        await ProcessOneDomainSshNginx(tencentJerry, "ITV-Chandao", "chandao.itvtech.cn", sshTargetITVDocker);
        await ProcessOneDomainSshNginx(tencentJerry, "ITV-Zentao", "zentao.itvtech.cn", sshTargetITVDocker);
        await ProcessOneDomainSshNginx(tencentJerry, "ITV-WWW", "www.itvtech.cn", sshTargetITVWWW);
        await ProcessOneDomainSshNginx(tencentWsy, "WSY-BwgLa", "la.bwg.wangshiyao.com", sshTargetWSYBwgLa);
        await ProcessOneDomainSshNginx(tencentWsy, "WSY-AliyunSH", "sh.aliyun.wangshiyao.com", sshTargetWSYAliyunSH);
        await ProcessOneDomainSshNginx(tencentWsy, "WSY-NTFY", "ntfy.wangshiyao.com", sshTargetWSYBwgLa);
        await ProcessOneDomainSshNginx(tencentWsy, "WSY-Shanghai", "shanghai.wangshiyao.com", sshTargetWSYDocker);
        await ProcessOneDomainSoftEther(tencentWsy, "WSY-Shanghai", "shanghai.wangshiyao.com", softEtherTargetWSYSH);
        await ProcessOneDomainSoftEther(tencentWsy, "WSY-AliyunSH", "sh.aliyun.wangshiyao.com", softEtherTargetWSYAliyunSH);
        await ProcessOneDomainSoftEther(tencentWsy, "WSY-BwgLA", "la.bwg.wangshiyao.com", softEtherTargetWSYBwgLA);
        await ProcessOneDomainSoftEther(tencentJerry, "ITV-Tianjin", "tianjin.itvtech.cn", softEtherTargetITVTianjin);
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
