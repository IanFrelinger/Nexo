using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Nexo.Infrastructure.Execution;

/// <summary>
/// DI extensions for brick host (options and optional remote catalogs).
/// </summary>
public static class BrickHostServiceCollectionExtensions
{
    /// <summary>
    /// Binds <see cref="BrickHostOptions"/> from configuration (section "BrickHost").
    /// Caller should register <see cref="CompositeBrickRegistry"/> when using remote catalogs:
    /// local <see cref="IBrickRegistry"/> + <see cref="IRemoteBrickCatalog"/> list + <see cref="HttpClient"/>.
    /// </summary>
    public static IServiceCollection AddNexoBrickHostOptions(
        this IServiceCollection services,
        Action<BrickHostOptions>? configure = null)
    {
        if (configure != null)
            services.Configure(configure);
        return services;
    }
}
