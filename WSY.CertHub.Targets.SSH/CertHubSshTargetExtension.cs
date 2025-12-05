global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;

global using WSY.CertHub.Core;
using WSY.CertHub.Targets.SSH;

namespace Microsoft.Extensions.DependencyInjection;

public static class CertHubSshTargetExtension
{
    public static IServiceCollection AddSshTargets(this IServiceCollection services, params IEnumerable<string> names)
    {
        foreach(var name in names)
        {
            SshTarget.ValidateServiceKey(name);
            services.AddKeyedSingleton<ITarget, SshTarget>(name);
        }
        return services;
    }
}
