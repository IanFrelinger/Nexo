using Microsoft.Extensions.Logging;
using Nexo.GeoTerrain;
using Nexo.GeoVector.Models;
using Nexo.GeoVector.Values;
using Nexo.GeoWorld.Results;
using Nexo.Orchestration.GeoVector.Ports;

namespace Nexo.Adapters.GeoVector.Providers;

/// <summary>
/// Wraps an IVectorProvider to support partial failure handling.
/// Supports batch operations with partial success reporting.
/// </summary>
public sealed class PartialFailureVectorProvider : IVectorProvider
{
    private readonly IVectorProvider _inner;
    private readonly ILogger<PartialFailureVectorProvider>? _logger;

    public PartialFailureVectorProvider(
        IVectorProvider inner,
        ILogger<PartialFailureVectorProvider>? logger = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PartialFailureVectorProvider>.Instance;
    }

    /// <summary>
    /// Gets features (standard interface).
    /// </summary>
    public Task<GeoFeatureSet> GetFeaturesAsync(GeoBounds bounds, FeatureKind kind, CancellationToken cancellationToken = default)
    {
        return _inner.GetFeaturesAsync(bounds, kind, cancellationToken);
    }

    /// <summary>
    /// Gets features from multiple bounds with partial failure support.
    /// </summary>
    public async Task<PartialResult<GeoFeatureSet>> GetFeaturesFromMultipleBoundsAsync(
        IReadOnlyList<GeoBounds> boundsList,
        FeatureKind kind,
        CancellationToken cancellationToken = default)
    {
        var results = new List<GeoFeatureSet>();
        var failures = new List<string>();

        foreach (var bounds in boundsList)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var features = await _inner.GetFeaturesAsync(bounds, kind, cancellationToken);
                results.Add(features);
            }
            catch (Exception ex)
            {
                var error = $"Failed to get features for bounds {bounds}: {ex.Message}";
                failures.Add(error);
                _logger?.LogWarning(ex, "Failed to get vector features for bounds {Bounds}", bounds);
            }
        }

        if (failures.Count == 0)
            return PartialResult.Successful(results);

        if (results.Count == 0)
            return PartialResult.Failed<GeoFeatureSet>(failures);

        return PartialResult.PartialSuccess(results, failures);
    }
}
