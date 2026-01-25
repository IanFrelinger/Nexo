using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Nexo.Adapters.GeoVector.Utilities;
using Nexo.GeoTerrain;
using Nexo.Orchestration.GeoVector.Ports;

namespace Nexo.Adapters.GeoVector.Providers;

/// <summary>
/// Factory for creating vector providers based on configuration.
/// </summary>
public class VectorProviderFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;

    public VectorProviderFactory(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <summary>
    /// Builds a vector provider based on the specified configuration.
    /// </summary>
    public IVectorProvider Build(
        string provider,
        GeoBounds bounds,
        string? mapboxAccessToken,
        string? mapboxTileset,
        int? mapboxZoom,
        string? osmPbfPath,
        bool airGapped,
        string? cacheRoot = null,
        bool persistCache = true)
    {
        provider = (provider ?? "echo").Trim().ToLowerInvariant();
        
        IVectorProvider offline = provider switch
        {
            "osm" or "hybrid" => string.IsNullOrWhiteSpace(osmPbfPath)
                ? throw new InvalidOperationException("OSM PBF path is required for --vector-provider osm|hybrid. Use --osm-pbf <path>.")
                : new OsmPbfVectorProvider(osmPbfPath, _loggerFactory.CreateLogger<OsmPbfVectorProvider>()),
            _ => new EchoVectorProvider()
        };

        if (provider == "osm" || provider == "echo")
        {
            return new CachedVectorProvider(offline);
        }

        MapboxVectorTileProvider? online = null;
        if (!airGapped)
        {
            online = new MapboxVectorTileProvider(
                _httpClientFactory.CreateClient("geovector.mapbox"),
                accessToken: MapboxTokenResolver.Resolve(mapboxAccessToken),
                tilesetId: string.IsNullOrWhiteSpace(mapboxTileset) ? "mapbox.mapbox-streets-v8" : mapboxTileset!,
                zoom: mapboxZoom ?? 15,
                formatExtension: "mvt",
                tileCacheRoot: cacheRoot,
                persistTileCache: persistCache,
                logger: _loggerFactory.CreateLogger<MapboxVectorTileProvider>());
        }

        return provider switch
        {
            "mapbox" => online is null
                ? throw new InvalidOperationException("Mapbox provider is not available in air-gapped mode.")
                : new CachedVectorProvider(online),
            "hybrid" => new CachedVectorProvider(new HybridVectorProvider(offline, online, _loggerFactory.CreateLogger<HybridVectorProvider>())),
            _ => throw new InvalidOperationException($"Unknown vector provider '{provider}'. Use echo|osm|mapbox|hybrid.")
        };
    }
}
