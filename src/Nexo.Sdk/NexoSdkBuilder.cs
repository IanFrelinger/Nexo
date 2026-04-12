using Microsoft.Extensions.DependencyInjection;
using Nexo.Client;

namespace Nexo.Sdk;

/// <summary>
/// Builder for configuring Nexo SDK options.
/// Use for Unity, Unreal, or embedded scenarios where local model or server can be used.
/// </summary>
public sealed class NexoSdkBuilder
{
    private readonly IServiceCollection _services;

    internal NexoSdkBuilder(IServiceCollection services)
    {
        _services = services;
    }
}

/// <summary>
/// Extension methods for adding Nexo SDK to dependency injection.
/// </summary>
public static class NexoSdkServiceCollectionExtensions
{
    /// <summary>
    /// Adds Nexo SDK (Nexo.Client) with optional adaptive configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="baseUrl">Base URL of the Nexo API (e.g. https://your-server:5000).</param>
    /// <param name="configure">Optional builder configuration.</param>
    public static IServiceCollection AddNexoSdk(
        this IServiceCollection services,
        string baseUrl,
        Action<NexoSdkBuilder>? configure = null)
    {
        services.AddNexoClient(c => c.BaseUrl = baseUrl);
        configure?.Invoke(new NexoSdkBuilder(services));
        return services;
    }
}
