using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Nexo.Orchestration.GeoTerrain.Ports;

namespace Nexo.Adapters.GeoTerrain.Providers;

/// <summary>
/// Factory for creating elevation providers based on configuration.
/// </summary>
public class ElevationProviderFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;

    public ElevationProviderFactory(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <summary>
    /// Builds an elevation provider based on the specified configuration.
    /// </summary>
    public IElevationProvider Build(
        string provider,
        string? localRoot,
        string? srtmBaseUrl,
        bool persistDownloads,
        bool enableCache,
        bool airGapped,
        string? cacheRoot = null,
        bool persistCache = true)
    {
        provider = (provider ?? "echo").Trim().ToLowerInvariant();

        if (airGapped && provider is "http" or "srtmhttp" or "hybrid")
        {
            provider = "local";
        }

        IElevationProvider inner = provider switch
        {
            "echo" => new EchoElevationProvider(),
            "local" => new LocalFileElevationProvider(localRoot),
            "http" or "srtmhttp" => new SrtmHttpElevationProvider(
                _httpClientFactory.CreateClient("geoterrain.srtm"),
                srtmBaseUrl,
                _loggerFactory.CreateLogger<SrtmHttpElevationProvider>(),
                cacheRoot: cacheRoot,
                persistCache: persistCache),
            "hybrid" => BuildHybrid(localRoot, srtmBaseUrl, persistDownloads, cacheRoot, persistCache),
            _ => throw new InvalidOperationException($"Unknown elevation provider '{provider}'. Use echo|local|http|hybrid.")
        };

        return enableCache ? new CachedElevationProvider(inner) : inner;
    }

    private IElevationProvider BuildHybrid(string? localRoot, string? srtmBaseUrl, bool persistDownloads, string? cacheRoot = null, bool persistCache = true)
    {
        if (string.IsNullOrWhiteSpace(localRoot))
        {
            // If no local root was provided, fall back to pure HTTP mode with caching.
            return new SrtmHttpElevationProvider(
                _httpClientFactory.CreateClient("geoterrain.srtm"),
                srtmBaseUrl,
                _loggerFactory.CreateLogger<SrtmHttpElevationProvider>(),
                cacheRoot: cacheRoot,
                persistCache: persistCache);
        }

        var local = new LocalFileElevationProvider(localRoot);
        var http = new SrtmHttpElevationProvider(
            _httpClientFactory.CreateClient("geoterrain.srtm"),
            srtmBaseUrl,
            _loggerFactory.CreateLogger<SrtmHttpElevationProvider>(),
            cacheRoot: cacheRoot,
            persistCache: persistCache);

        return new HybridLocalThenHttpElevationProvider(
            local,
            http,
            persistDownloads,
            _loggerFactory.CreateLogger<HybridLocalThenHttpElevationProvider>());
    }
}
