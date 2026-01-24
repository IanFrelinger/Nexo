using Nexo.GeoTerrain;

namespace Nexo.GeoVector.Geometry;

/// <summary>
/// Minimal geometry abstraction for vector features (polygons, polylines, etc.).
/// </summary>
public interface IGeoGeometry
{
    /// <summary>
    /// Representative points for filtering/placement. For polygons, this is the outer ring.
    /// For polylines, this is the line vertices.
    /// </summary>
    IReadOnlyList<GeoPoint> Points { get; }
}

