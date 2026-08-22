using Microsoft.Extensions.DependencyInjection;
using Ashlar.Client;

namespace Ashlar.Sdk.Client;

/// <summary>
/// DI extensions for the slim NuGet <c>Ashlar.Sdk</c> package (remote API client).
/// </summary>
public static class AshlarClientSdkServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IAshlarClient"/> and optional client-side SDK hooks against the given Ashlar API base URL.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="baseUrl">Base URL of the Ashlar API (e.g. https://your-server:5000).</param>
    /// <param name="configure">Optional builder configuration.</param>
    public static IServiceCollection AddAshlarClientSdk(
        this IServiceCollection services,
        string baseUrl,
        Action<AshlarClientSdkBuilder>? configure = null)
    {
        services.AddAshlarClient(c => c.BaseUrl = baseUrl);
        configure?.Invoke(new AshlarClientSdkBuilder(services));
        return services;
    }
}
