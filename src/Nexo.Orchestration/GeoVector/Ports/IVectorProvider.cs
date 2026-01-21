using Nexo.GeoTerrain;
using Nexo.GeoVector.Models;
using Nexo.GeoVector.Values;

namespace Nexo.Orchestration.GeoVector.Ports;

/// <summary>
/// Port for retrieving vector features (e.g., buildings/roads/vegetation) for a region.
/// Implementations live in adapters (HTTP, local cache, etc.).
/// </summary>
public interface IVectorProvider
{
    Task<GeoFeatureSet> GetFeaturesAsync(GeoBounds bounds, FeatureKind kind, CancellationToken cancellationToken = default);
}

