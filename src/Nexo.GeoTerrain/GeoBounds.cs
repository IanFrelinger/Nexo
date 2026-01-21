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
}

