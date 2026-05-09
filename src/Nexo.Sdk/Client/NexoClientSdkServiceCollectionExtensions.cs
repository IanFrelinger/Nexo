using Microsoft.Extensions.DependencyInjection;
using Nexo.Client;

namespace Nexo.Sdk.Client;

/// <summary>
/// DI extensions for the slim NuGet <c>Nexo.Sdk</c> package (remote API client).
/// </summary>
public static class NexoClientSdkServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="INexoClient"/> and optional client-side SDK hooks against the given Nexo API base URL.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="baseUrl">Base URL of the Nexo API (e.g. https://your-server:5000).</param>
    /// <param name="configure">Optional builder configuration.</param>
    public static IServiceCollection AddNexoClientSdk(
        this IServiceCollection services,
        string baseUrl,
        Action<NexoClientSdkBuilder>? configure = null)
    {
        services.AddNexoClient(c => c.BaseUrl = baseUrl);
        configure?.Invoke(new NexoClientSdkBuilder(services));
        return services;
    }
}
