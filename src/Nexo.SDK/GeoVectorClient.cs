using Microsoft.Extensions.Logging;
using Nexo.GeoTerrain;
using Nexo.GeoVector.Models;
using Nexo.GeoVector.Values;
using Nexo.Orchestration.GeoVector.Ports;
using Nexo.SDK.Estimation;

namespace Nexo.SDK;

/// <summary>
/// Client for vector feature extraction operations.
/// Provides programmatic access to vector feature extraction without shelling out to CLI.
/// </summary>
public class GeoVectorClient
{
    private readonly IVectorProvider _vectorProvider;
    private readonly ILogger<GeoVectorClient>? _logger;
    private readonly IResourceEstimator? _estimator;

    /// <summary>
    /// Initializes a new instance of the GeoVectorClient.
    /// </summary>
    /// <param name="vectorProvider">Vector data provider</param>
    /// <param name="logger">Optional logger</param>
    /// <param name="estimator">Optional resource estimator for cost and memory tracking</param>
    public GeoVectorClient(
        IVectorProvider vectorProvider, 
        ILogger<GeoVectorClient>? logger = null,
        IResourceEstimator? estimator = null)
    {
        _vectorProvider = vectorProvider ?? throw new ArgumentNullException(nameof(vectorProvider));
        _logger = logger;
        _estimator = estimator;
    }

    /// <summary>
    /// Extract vector features from geographic bounds.
    /// </summary>
    /// <param name="bounds">Geographic bounds</param>
    /// <param name="kind">Feature kind (building, road, water, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Extracted features</returns>
    public async Task<GeoFeatureSet> ExtractFeaturesAsync(
        GeoBounds bounds,
        FeatureKind kind,
        CancellationToken cancellationToken = default)
    {
        if (bounds == null)
            throw new ArgumentNullException(nameof(bounds));
        if (kind == null)
            throw new ArgumentNullException(nameof(kind));

        bounds.Validate();

        _logger?.LogInformation("Extracting {Kind} features from bounds {Bounds}", kind.Value, bounds);

        var features = await _vectorProvider.GetFeaturesAsync(bounds, kind, cancellationToken);
        
        _logger?.LogInformation("Extracted {Count} features", features.Features.Count);

        return features;
    }

    /// <summary>
    /// Extract vector features from geographic bounds with resource estimation.
    /// </summary>
    /// <param name="bounds">Geographic bounds</param>
    /// <param name="kind">Feature kind (building, road, water, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Extracted features with resource estimates</returns>
    public async Task<EstimationResult<GeoFeatureSet>> ExtractFeaturesWithEstimationAsync(
        GeoBounds bounds,
        FeatureKind kind,
        CancellationToken cancellationToken = default)
    {
        if (bounds == null)
            throw new ArgumentNullException(nameof(bounds));
        if (kind == null)
            throw new ArgumentNullException(nameof(kind));

        bounds.Validate();

        ResourceEstimate? estimate = null;
        ResourceEstimate? actual = null;

        // Generate estimate before operation
        if (_estimator != null)
        {
            // Estimate Mapbox vector tile download (assuming zoom level 15)
            var vectorTileEstimate = _estimator.EstimateMapboxVectorTileDownload(bounds, 15);
            
            // Estimate feature memory (conservative: assume 100 features)
            var featureEstimate = _estimator.EstimateVectorFeaturesMemory(100);
            
            estimate = vectorTileEstimate + featureEstimate;
            
            _logger?.LogInformation("Estimated cost: ${Cost:F4}, memory: {Memory:F2} MB", 
                estimate.CostUsd, estimate.MemoryMegabytes);
        }

        // Perform actual operation
        var features = await ExtractFeaturesAsync(bounds, kind, cancellationToken);

        // Calculate actual resource usage
        if (_estimator != null)
        {
            // Actual cost is same as estimate (based on tile requests)
            var actualDownload = _estimator.EstimateMapboxVectorTileDownload(bounds, 15);
            
            // Calculate actual feature memory
            var actualFeatureMemory = MemoryCalculator.CalculateVectorFeaturesMemory(features);
            var actualFeatures = new ResourceEstimate
            {
                CostUsd = 0,
                MemoryBytes = actualFeatureMemory,
                IsEstimate = false,
                MemoryBreakdown = new Dictionary<string, long>
                {
                    ["vector-features"] = actualFeatureMemory
                }
            };
            
            actual = actualDownload with { IsEstimate = false } + actualFeatures;
            
            _logger?.LogInformation("Actual cost: ${Cost:F4}, memory: {Memory:F2} MB", 
                actual.CostUsd, actual.MemoryMegabytes);
        }

        return new EstimationResult<GeoFeatureSet>
        {
            Result = features,
            Estimate = estimate,
            Actual = actual
        };
    }

    /// <summary>
    /// Export features to a file.
    /// </summary>
    /// <param name="features">Features to export</param>
    /// <param name="filePath">Output file path</param>
    /// <param name="format">Export format (json, geojson)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task ExportFeaturesAsync(
        GeoFeatureSet features,
        string filePath,
        string format = "json",
        CancellationToken cancellationToken = default)
    {
        if (features == null)
            throw new ArgumentNullException(nameof(features));
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path is required.", nameof(filePath));

        format = format.ToLowerInvariant();
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        switch (format)
        {
            case "json":
                // TODO: Implement JSON export
                throw new NotImplementedException("JSON export not yet implemented in SDK");
            case "geojson":
                // TODO: Implement GeoJSON export
                throw new NotImplementedException("GeoJSON export not yet implemented in SDK");
            default:
                throw new NotSupportedException($"Unsupported export format: {format}");
        }
    }
}
