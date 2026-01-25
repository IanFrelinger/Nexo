namespace Nexo.SDK.Estimation;

/// <summary>
/// Cost models for different data providers and services.
/// </summary>
public static class CostModels
{
    /// <summary>
    /// Default Mapbox Vector Tiles API cost per tile request (approximate, varies by tier).
    /// Free tier: 200,000 requests/month, then ~$0.0005 per request.
    /// </summary>
    public const decimal MapboxVectorTilePerRequest = 0.0005m;

    /// <summary>
    /// Default Mapbox Raster Tiles API cost per tile request (approximate, varies by tier).
    /// </summary>
    public const decimal MapboxRasterTilePerRequest = 0.0005m;

    /// <summary>
    /// SRTM data is typically free, but bandwidth costs may apply.
    /// Estimated at $0.01 per GB transferred.
    /// </summary>
    public const decimal SrtmBandwidthPerGb = 0.01m;

    /// <summary>
    /// OSM PBF files are free, but bandwidth/storage costs may apply.
    /// Estimated at $0.01 per GB transferred.
    /// </summary>
    public const decimal OsmBandwidthPerGb = 0.01m;

    /// <summary>
    /// Default cost model configuration.
    /// </summary>
    public static CostModelConfiguration Default { get; } = new CostModelConfiguration
    {
        MapboxVectorTileCostPerRequest = MapboxVectorTilePerRequest,
        MapboxRasterTileCostPerRequest = MapboxRasterTilePerRequest,
        SrtmBandwidthCostPerGb = SrtmBandwidthPerGb,
        OsmBandwidthCostPerGb = OsmBandwidthPerGb
    };
}

/// <summary>
/// Configuration for cost models, allowing customization of pricing.
/// </summary>
public class CostModelConfiguration
{
    /// <summary>
    /// Cost per Mapbox vector tile request in USD.
    /// </summary>
    public decimal MapboxVectorTileCostPerRequest { get; set; } = CostModels.MapboxVectorTilePerRequest;

    /// <summary>
    /// Cost per Mapbox raster tile request in USD.
    /// </summary>
    public decimal MapboxRasterTileCostPerRequest { get; set; } = CostModels.MapboxRasterTilePerRequest;

    /// <summary>
    /// Cost per GB of SRTM data transferred in USD.
    /// </summary>
    public decimal SrtmBandwidthCostPerGb { get; set; } = CostModels.SrtmBandwidthPerGb;

    /// <summary>
    /// Cost per GB of OSM data transferred in USD.
    /// </summary>
    public decimal OsmBandwidthCostPerGb { get; set; } = CostModels.OsmBandwidthPerGb;
}
