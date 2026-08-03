using Microsoft.Extensions.DependencyInjection;
using Nexo.Hosting;

namespace Nexo.Framework.Sdk;

/// <summary>
/// Aggregates stable Nexo integration extension methods for hybrid or quick bootstrap scenarios.
/// </summary>
public static class NexoFrameworkServiceCollectionExtensions
{
    /// <summary>
    /// Optionally registers the HTTP Nexo client and/or the in-process kernel.
    /// </summary>
    public static IServiceCollection AddNexoFramework(
        this IServiceCollection services,
        Action<NexoFrameworkOptions>? configure = null)
    {
        var opt = new NexoFrameworkOptions();
        configure?.Invoke(opt);

        if (!string.IsNullOrWhiteSpace(opt.RemoteApiBaseUrl))
            Nexo.Sdk.Client.NexoClientSdkServiceCollectionExtensions.AddNexoClientSdk(services, opt.RemoteApiBaseUrl);

        if (opt.RegisterKernel)
            Nexo.Hosting.NexoServiceCollectionExtensions.AddNexo(services, opt.ConfigureHost);

        return services;
    }
}
