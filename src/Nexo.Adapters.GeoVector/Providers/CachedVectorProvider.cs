using Nexo.GeoTerrain;
using Nexo.GeoVector.Models;
using Nexo.GeoVector.Values;
using Nexo.Orchestration.GeoVector.Ports;

namespace Nexo.Adapters.GeoVector.Providers;

/// <summary>
/// In-memory cache decorator for vector providers (useful for multi-step pipelines).
/// </summary>
public sealed class CachedVectorProvider : IVectorProvider
{
    private readonly IVectorProvider _inner;
    private readonly Dictionary<string, GeoFeatureSet> _cache = new(StringComparer.Ordinal);

    public CachedVectorProvider(IVectorProvider inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public async Task<GeoFeatureSet> GetFeaturesAsync(GeoBounds bounds, FeatureKind kind, CancellationToken cancellationToken = default)
    {
        bounds.Validate();
        kind ??= FeatureKind.Building;

        var key = $"{kind.Value}|{bounds.MinLatitude.Degrees:F6},{bounds.MinLongitude.Degrees:F6},{bounds.MaxLatitude.Degrees:F6},{bounds.MaxLongitude.Degrees:F6}";
        if (_cache.TryGetValue(key, out var hit)) return hit;

        var got = await _inner.GetFeaturesAsync(bounds, kind, cancellationToken);
        _cache[key] = got;
        return got;
    }
}

