using Ashlar.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Ashlar.Workstation;

/// <summary>
/// Product composition for the air-gapped workstation / IDE daemon.
/// Uses <see cref="AshlarDeploymentProfile.SecureWorkstation"/> and turns
/// trust on — the framework profile registers trust services, the product
/// opts into enforcing them.
/// </summary>
public static class WorkstationHostBuilder
{
    /// <summary>
    /// Registers the Ashlar kernel with the SecureWorkstation module set.
    /// </summary>
    /// <param name="services">Service collection to compose.</param>
    /// <param name="configure">Optional additional hosting options.</param>
    /// <returns>The same <paramref name="services"/> instance.</returns>
    public static IServiceCollection AddAshlarWorkstation(
        this IServiceCollection services,
        Action<AshlarHostingOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAshlarProfile(AshlarDeploymentProfile.SecureWorkstation, options =>
        {
            configure?.Invoke(options);
            options.TrustEnabled = true;
            options.DeploymentProfile = AshlarDeploymentProfile.SecureWorkstation;
        });

        return services;
    }
}
