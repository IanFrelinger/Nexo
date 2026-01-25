using Nexo.GeoTerrain;
using Nexo.GeoVector.Models;

namespace Nexo.SDK.Estimation;

/// <summary>
/// Interface for estimating resource usage (cost and memory) for geospatial operations.
/// </summary>
public interface IResourceEstimator
{
    /// <summary>
    /// Estimates cost and memory for downloading SRTM elevation tiles.
    /// </summary>
    /// <param name="tileIds">List of SRTM tile IDs</param>
    /// <param name="fromCache">Whether all tiles are from cache (no cost)</param>
    /// <param name="cacheRoot">Optional cache directory root to check for cached tiles</param>
    ResourceEstimate EstimateSrtmTileDownload(IReadOnlyList<SrtmTileId> tileIds, bool fromCache = false, string? cacheRoot = null);

    /// <summary>
    /// Estimates cost and memory for downloading Mapbox vector tiles.
    /// </summary>
    ResourceEstimate EstimateMapboxVectorTileDownload(GeoBounds bounds, int zoomLevel, int? estimatedTileCount = null);

    /// <summary>
    /// Estimates cost and memory for downloading Mapbox raster tiles.
    /// </summary>
    ResourceEstimate EstimateMapboxRasterTileDownload(GeoBounds bounds, int zoomLevel, int? estimatedTileCount = null);

    /// <summary>
    /// Estimates memory footprint for an elevation grid.
    /// </summary>
    ResourceEstimate EstimateElevationGridMemory(int width, int height);

    /// <summary>
    /// Estimates memory footprint for a mesh.
    /// </summary>
    ResourceEstimate EstimateMeshMemory(int vertexCount, int indexCount, bool hasNormals = true, bool hasTexCoords = true);

    /// <summary>
    /// Estimates memory footprint for vector features.
    /// </summary>
    ResourceEstimate EstimateVectorFeaturesMemory(int featureCount, int? estimatedVertexCount = null);

    /// <summary>
    /// Estimates cost and memory for downloading OSM PBF data.
    /// </summary>
    ResourceEstimate EstimateOsmPbfDownload(long fileSizeBytes);
}
