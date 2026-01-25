using Nexo.GeoTerrain;

namespace Nexo.SDK.Estimation;

/// <summary>
/// Helper methods for resource estimation.
/// </summary>
public static class EstimationHelpers
{
    /// <summary>
    /// Estimates total resources needed for generating a terrain mesh from bounds.
    /// </summary>
    public static ResourceEstimate EstimateTerrainMeshGeneration(
        IResourceEstimator estimator,
        GeoBounds bounds,
        int? estimatedGridWidth = null,
        int? estimatedGridHeight = null)
    {
        if (estimator == null)
            throw new ArgumentNullException(nameof(estimator));
        if (bounds == null)
            throw new ArgumentNullException(nameof(bounds));

        bounds.Validate();

        // Estimate tile downloads
        var tileIds = SrtmTileCoverage.TilesCovering(bounds);
        var downloadEstimate = estimator.EstimateSrtmTileDownload(tileIds);

        // Estimate grid size
        var gridWidth = estimatedGridWidth ?? 1000;
        var gridHeight = estimatedGridHeight ?? 1000;
        var gridEstimate = estimator.EstimateElevationGridMemory(gridWidth, gridHeight);

        // Estimate mesh (2 triangles per grid cell)
        var estimatedTriangles = (gridWidth - 1) * (gridHeight - 1) * 2;
        var estimatedVertices = gridWidth * gridHeight;
        var meshEstimate = estimator.EstimateMeshMemory(estimatedVertices, estimatedTriangles * 3);

        return downloadEstimate + gridEstimate + meshEstimate;
    }

    /// <summary>
    /// Estimates total resources needed for extracting vector features.
    /// </summary>
    public static ResourceEstimate EstimateVectorFeatureExtraction(
        IResourceEstimator estimator,
        GeoBounds bounds,
        int zoomLevel = 15,
        int? estimatedFeatureCount = null)
    {
        if (estimator == null)
            throw new ArgumentNullException(nameof(estimator));
        if (bounds == null)
            throw new ArgumentNullException(nameof(bounds));

        bounds.Validate();

        // Estimate Mapbox tile downloads
        var tileEstimate = estimator.EstimateMapboxVectorTileDownload(bounds, zoomLevel);

        // Estimate feature memory
        var featureCount = estimatedFeatureCount ?? 100; // Conservative default
        var featureEstimate = estimator.EstimateVectorFeaturesMemory(featureCount);

        return tileEstimate + featureEstimate;
    }

    /// <summary>
    /// Formats a resource estimate as a human-readable string.
    /// </summary>
    public static string FormatEstimate(ResourceEstimate? estimate)
    {
        if (estimate == null)
            return "No estimate available";

        var type = estimate.IsEstimate ? "Estimate" : "Actual";
        var cost = estimate.CostUsd > 0 
            ? $"${estimate.CostUsd:F4}" 
            : "Free";
        var memory = estimate.MemoryBytes > 1024 * 1024 * 1024
            ? $"{estimate.MemoryGigabytes:F2} GB"
            : $"{estimate.MemoryMegabytes:F2} MB";

        return $"{type}: Cost = {cost}, Memory = {memory}";
    }
}
