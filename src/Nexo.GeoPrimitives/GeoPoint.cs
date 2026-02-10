namespace Nexo.GeoTerrain;

/// <summary>
/// Geographic point in WGS84 degrees (latitude/longitude).
/// </summary>
public sealed record GeoPoint
{
    public required Latitude Latitude { get; init; }
    public required Longitude Longitude { get; init; }

    public override string ToString() => $"{Latitude.Degrees:F6},{Longitude.Degrees:F6}";
}
