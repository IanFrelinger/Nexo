using Microsoft.Extensions.DependencyInjection;
using Nexo.Hosting;

namespace Nexo.Framework.Sdk;

/// <summary>
/// Single visible option bag for apps that want client + host wiring without importing multiple extension namespaces.
/// </summary>
public sealed class NexoFrameworkOptions
{
    /// <summary>
    /// When set, registers <c>AddNexoClientSdk</c> from <c>Nexo.Sdk.Client</c>.
    /// Remote-only apps can skip <see cref="RegisterKernel"/>.
    /// </summary>
    public string? RemoteApiBaseUrl { get; set; }

    /// Forwarded to Hosting <c>AddNexo</c>.
    public Action<NexoHostingOptions>? ConfigureHost { get; set; }

    /// <summary>When true (default), registers the full Nexo kernel via <c>AddNexo</c>.</summary>
    public bool RegisterKernel { get; set; } = true;
}

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
