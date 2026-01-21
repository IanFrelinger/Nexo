using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexo.Adapters.GeoTerrain.Providers;
using Nexo.Orchestration.GeoTerrain.Ports;

namespace Nexo.Adapters.GeoTerrain;

/// <summary>
/// DI registration for elevation providers, mirroring <c>Nexo.Adapters.Assets</c> patterns.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddElevationProviders(
        this IServiceCollection services,
        Action<ElevationProviderOptions>? configure = null)
    {
        var options = new ElevationProviderOptions();
        configure?.Invoke(options);

        // Ensure IHttpClientFactory is available for HTTP providers.
        services.AddHttpClient();

        // We avoid Scrutor "Decorate" to keep dependencies minimal; instead we register
        // the chosen provider as a concrete type and optionally wrap it with caching.
        switch (options.Provider)
        {
            case ElevationProviderType.SrtmHttp:
                services.TryAddSingleton<SrtmHttpElevationProvider>(sp =>
                {
                    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("geoterrain.srtm");
                    var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SrtmHttpElevationProvider>>();
                    return new SrtmHttpElevationProvider(http, options.SrtmBaseUrl, logger);
                });
                services.AddSingleton<IElevationProvider>(sp =>
                {
                    IElevationProvider inner = sp.GetRequiredService<SrtmHttpElevationProvider>();
                    return options.EnableCache ? new CachedElevationProvider(inner) : inner;
                });
                break;
            case ElevationProviderType.Local:
                services.AddSingleton<LocalFileElevationProvider>(_ => new LocalFileElevationProvider(options.LocalRootPath));
                services.AddSingleton<IElevationProvider>(sp =>
                {
                    IElevationProvider inner = sp.GetRequiredService<LocalFileElevationProvider>();
                    return options.EnableCache ? new CachedElevationProvider(inner) : inner;
                });
                break;
            case ElevationProviderType.HybridLocalThenHttp:
                services.AddSingleton<LocalFileElevationProvider>(_ => new LocalFileElevationProvider(options.LocalRootPath));
                services.TryAddSingleton<SrtmHttpElevationProvider>(sp =>
                {
                    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("geoterrain.srtm");
                    var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SrtmHttpElevationProvider>>();
                    return new SrtmHttpElevationProvider(http, options.SrtmBaseUrl, logger);
                });
                services.AddSingleton<HybridLocalThenHttpElevationProvider>(sp =>
                {
                    return new HybridLocalThenHttpElevationProvider(
                        sp.GetRequiredService<LocalFileElevationProvider>(),
                        sp.GetRequiredService<SrtmHttpElevationProvider>(),
                        options.PersistDownloadedTilesToLocal,
                        sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<HybridLocalThenHttpElevationProvider>>());
                });
                services.AddSingleton<IElevationProvider>(sp =>
                {
                    IElevationProvider inner = sp.GetRequiredService<HybridLocalThenHttpElevationProvider>();
                    return options.EnableCache ? new CachedElevationProvider(inner) : inner;
                });
                break;
            default:
                services.AddSingleton<EchoElevationProvider>();
                services.AddSingleton<IElevationProvider>(sp =>
                {
                    IElevationProvider inner = sp.GetRequiredService<EchoElevationProvider>();
                    return options.EnableCache ? new CachedElevationProvider(inner) : inner;
                });
                break;
        }

        return services;
    }
}

public sealed class ElevationProviderOptions
{
    public ElevationProviderType Provider { get; set; } = ElevationProviderType.Echo;
    public bool EnableCache { get; set; } = true;

    /// <summary>
    /// Root directory containing SRTM .hgt files named like N37W122.hgt (used by Local provider).
    /// </summary>
    public string? LocalRootPath { get; set; }

    /// <summary>
    /// Base URL for SRTM downloads, e.g. <c>https://example.com/srtm/</c>.
    /// Used by HTTP and Hybrid providers.
    /// </summary>
    public string? SrtmBaseUrl { get; set; }

    /// <summary>
    /// If true and using Hybrid provider, download-missed tiles are persisted into <see cref="LocalRootPath"/>.
    /// </summary>
    public bool PersistDownloadedTilesToLocal { get; set; } = true;
}

public enum ElevationProviderType
{
    Echo,
    Local,
    SrtmHttp,
    HybridLocalThenHttp
}

