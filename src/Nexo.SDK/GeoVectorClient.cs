using Microsoft.Extensions.Logging;
using Nexo.GeoTerrain;
using Nexo.GeoVector.Models;
using Nexo.GeoVector.Values;
using Nexo.Orchestration.GeoVector.Ports;

namespace Nexo.SDK;

/// <summary>
/// Client for vector feature extraction operations.
/// Provides programmatic access to vector feature extraction without shelling out to CLI.
/// </summary>
public class GeoVectorClient
{
    private readonly IVectorProvider _vectorProvider;
    private readonly ILogger<GeoVectorClient>? _logger;

    /// <summary>
    /// Initializes a new instance of the GeoVectorClient.
    /// </summary>
    /// <param name="vectorProvider">Vector data provider</param>
    /// <param name="logger">Optional logger</param>
    public GeoVectorClient(IVectorProvider vectorProvider, ILogger<GeoVectorClient>? logger = null)
    {
        _vectorProvider = vectorProvider ?? throw new ArgumentNullException(nameof(vectorProvider));
        _logger = logger;
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
