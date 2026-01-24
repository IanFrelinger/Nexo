namespace Nexo.GeoTerrain;

/// <summary>
/// Geographic bounds in WGS84 degrees (latitude/longitude).
/// </summary>
public sealed record GeoBounds
{
    public required Latitude MinLatitude { get; init; }
    public required Latitude MaxLatitude { get; init; }
    public required Longitude MinLongitude { get; init; }
    public required Longitude MaxLongitude { get; init; }

    public double HeightDegrees => MaxLatitude.Degrees - MinLatitude.Degrees;
    public double WidthDegrees => MaxLongitude.Degrees - MinLongitude.Degrees;

    /// <summary>
    /// Approximate width in meters using average latitude.
    /// </summary>
    public double WidthMeters()
    {
        var avgLat = (MinLatitude.Degrees + MaxLatitude.Degrees) / 2.0;
        var latRad = avgLat * Math.PI / 180.0;
        var metersPerDegreeLongitude = 111320.0 * Math.Cos(latRad);
        return WidthDegrees * metersPerDegreeLongitude;
    }

    /// <summary>
    /// Approximate height in meters.
    /// </summary>
    public double HeightMeters()
    {
        return HeightDegrees * 111320.0; // Approximately constant meters per degree latitude
    }

    public void Validate()
    {
        if (MaxLatitude.Degrees < MinLatitude.Degrees)
            throw new InvalidOperationException("MaxLatitude must be >= MinLatitude.");
        if (MaxLongitude.Degrees < MinLongitude.Degrees)
            throw new InvalidOperationException("MaxLongitude must be >= MinLongitude.");

        // We intentionally do not handle antimeridian wrap here; treat that as a future enhancement.
    }

    public bool Contains(GeoPoint p)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(p);
#else
        if (p is null) throw new ArgumentNullException(nameof(p));
#endif
        Validate();
        return p.Latitude.Degrees >= MinLatitude.Degrees &&
               p.Latitude.Degrees <= MaxLatitude.Degrees &&
               p.Longitude.Degrees >= MinLongitude.Degrees &&
               p.Longitude.Degrees <= MaxLongitude.Degrees;
    }

    /// <summary>
    /// Checks if two bounds intersect.
    /// </summary>
    public bool Intersects(GeoBounds other)
    {
        if (other == null) return false;
        Validate();
        other.Validate();
        return MinLatitude.Degrees <= other.MaxLatitude.Degrees &&
               MaxLatitude.Degrees >= other.MinLatitude.Degrees &&
               MinLongitude.Degrees <= other.MaxLongitude.Degrees &&
               MaxLongitude.Degrees >= other.MinLongitude.Degrees;
    }
}

