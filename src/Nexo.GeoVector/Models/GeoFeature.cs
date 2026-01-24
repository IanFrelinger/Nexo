using Nexo.GeoVector.Geometry;
using Nexo.GeoVector.Values;

namespace Nexo.GeoVector.Models;

/// <summary>
/// A single vector feature (geometry + attributes).
/// </summary>
public sealed class GeoFeature
{
    public string Id { get; }
    public FeatureKind Kind { get; }
    public IGeoGeometry Geometry { get; }
    public IReadOnlyDictionary<string, object> Properties { get; }

    public GeoFeature(string id, FeatureKind kind, IGeoGeometry geometry, IReadOnlyDictionary<string, object>? properties = null)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Feature id is required.", nameof(id));
        Id = id;
        Kind = kind ?? throw new ArgumentNullException(nameof(kind));
        Geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
        Properties = properties ?? new Dictionary<string, object>();
    }
}

