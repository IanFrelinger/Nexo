namespace Nexo.GeoTerrain.Projection;

/// <summary>
/// Projects geographic coordinates (WGS84 lat/lon degrees) into a local metric 2D frame
/// (X east, Y north), relative to an origin.
/// </summary>
public interface ICoordinateProjector
{
    /// <summary>
    /// Projection identifier, e.g. "local_equirectangular", "utm", "webmercator".
    /// </summary>
    string ProjectionId { get; }

    /// <summary>
    /// Optional parameters describing this projector instance (e.g. UTM zone).
    /// </summary>
    IReadOnlyDictionary<string, string>? Parameters { get; }

    /// <summary>
    /// Project a geo point into local meters relative to <paramref name="origin"/>.
    /// </summary>
    Vector2 ProjectMeters(GeoPoint point, GeoPoint origin);
}

