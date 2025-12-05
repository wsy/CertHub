global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;

global using TencentCloud.Common;
global using WSY.CertHub.Core;
using WSY.CertHub.TencentCloud;
using WSY.CertHub.TencentCloud.Cert;
using WSY.CertHub.TencentCloud.Dns;

namespace Microsoft.Extensions.DependencyInjection;

public static class CertHubTencentCloudExtension
{
    public static IServiceCollection AddTencentCloudCertificateProvider(this IServiceCollection services, params IEnumerable<string> names)
    {
        foreach(var name in names)
        {
            TencentCloudCertificateProvider.ValidateServiceKey(name);
            services.AddKeyedSingleton<ICertificateProvider, TencentCloudCertificateProvider>(name);
        }
        return services;
    }
    public static IServiceCollection AddTencentCloudDnsProvider(this IServiceCollection services, params IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            TencentCloudDnsProvider.ValidateServiceKey(name);
            services.AddKeyedSingleton<IDnsProvider, TencentCloudDnsProvider>(name);
        }
        return services;
    }
}
