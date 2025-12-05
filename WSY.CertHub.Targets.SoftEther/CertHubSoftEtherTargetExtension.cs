global using System.Text.Json;

global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;

global using WSY.CertHub.Core;

using WSY.CertHub.Targets.SoftEther;

namespace Microsoft.Extensions.DependencyInjection;

public static class CertHubSoftEtherTargetExtension
{
    public static IServiceCollection AddSoftEtherTargets(this IServiceCollection services, params IEnumerable<string> names)
    {
        foreach(var name in names)
        {
            SoftEtherTarget.ValidateServiceKey(name);
            services.AddKeyedSingleton<ITarget, SoftEtherTarget>(name);
        }
        return services;
    }
}
