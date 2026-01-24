namespace Nexo.GeoTerrain.Projection;

/// <summary>
/// State Plane Coordinate System projector.
/// US-specific coordinate systems for individual states.
/// Uses Lambert Conformal Conic or Transverse Mercator depending on state.
/// </summary>
public sealed class StatePlaneProjector : ICoordinateProjector
{
    private readonly ICoordinateProjector _innerProjector;
    private readonly string _stateCode;
    private readonly IReadOnlyDictionary<string, string> _parameters;

    public StatePlaneProjector(string stateCode, int zone = 0)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            throw new ArgumentException("State code is required.", nameof(stateCode));

        _stateCode = stateCode.ToUpperInvariant();
        _innerProjector = CreateProjectorForState(_stateCode, zone);
        _parameters = new Dictionary<string, string>
        {
            ["stateCode"] = _stateCode,
            ["zone"] = zone.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
    }

    public string ProjectionId => "state_plane";

    public IReadOnlyDictionary<string, string>? Parameters => _parameters;

    public Vector2 ProjectMeters(GeoPoint point, GeoPoint origin)
    {
        return _innerProjector.ProjectMeters(point, origin);
    }

    private static ICoordinateProjector CreateProjectorForState(string stateCode, int zone)
    {
        // Common State Plane configurations
        // This is a simplified implementation - full State Plane has many zones
        return stateCode switch
        {
            "CA" => new LambertConformalConicProjector(34.0, 40.5, -120.0, 36.5), // California Zone 1
            "TX" => new LambertConformalConicProjector(27.5, 35.0, -98.0, 31.0),  // Texas Central
            "NY" => new LambertConformalConicProjector(40.0, 41.0, -74.0, 40.5),  // New York Long Island
            "FL" => new LambertConformalConicProjector(24.0, 31.0, -81.0, 24.0),  // Florida East
            _ => UtmProjector.CreateForBounds(
                new GeoBounds
                {
                    MinLatitude = new Latitude(25.0),
                    MaxLatitude = new Latitude(50.0),
                    MinLongitude = new Longitude(-125.0),
                    MaxLongitude = new Longitude(-65.0)
                }) // Default to UTM for unknown states
        };
    }
}
