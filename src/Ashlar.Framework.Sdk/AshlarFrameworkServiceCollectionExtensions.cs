using Microsoft.Extensions.DependencyInjection;
using Ashlar.Hosting;

namespace Ashlar.Framework.Sdk;

/// <summary>
/// Aggregates stable Ashlar integration extension methods for hybrid or quick bootstrap scenarios.
/// </summary>
public static class AshlarFrameworkServiceCollectionExtensions
{
    /// <summary>
    /// Optionally registers the HTTP Ashlar client and/or the in-process kernel.
    /// </summary>
    public static IServiceCollection AddAshlarFramework(
        this IServiceCollection services,
        Action<AshlarFrameworkOptions>? configure = null)
    {
        var opt = new AshlarFrameworkOptions();
        configure?.Invoke(opt);

        if (!string.IsNullOrWhiteSpace(opt.RemoteApiBaseUrl))
            Ashlar.Sdk.Client.AshlarClientSdkServiceCollectionExtensions.AddAshlarClientSdk(services, opt.RemoteApiBaseUrl);

        if (opt.RegisterKernel)
            Ashlar.Hosting.AshlarServiceCollectionExtensions.AddAshlar(services, opt.ConfigureHost);

        return services;
    }
}
