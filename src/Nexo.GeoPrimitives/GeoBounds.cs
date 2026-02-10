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
    /// Gets the center point of the bounds.
    /// </summary>
    public GeoPoint Center => new GeoPoint
    {
        Latitude = new Latitude((MinLatitude.Degrees + MaxLatitude.Degrees) * 0.5),
        Longitude = new Longitude((MinLongitude.Degrees + MaxLongitude.Degrees) * 0.5)
    };

    /// <summary>
    /// Parses bounds from a comma-separated string: "minLat,minLon,maxLat,maxLon"
    /// </summary>
    public static GeoBounds Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("bounds is required (minLat,minLon,maxLat,maxLon).", nameof(text));

        var parts = text.Split(',');
        if (parts.Length != 4)
            throw new ArgumentException("bounds must be 'minLat,minLon,maxLat,maxLon'.", nameof(text));

        var minLat = double.Parse(parts[0].Trim(), System.Globalization.CultureInfo.InvariantCulture);
        var minLon = double.Parse(parts[1].Trim(), System.Globalization.CultureInfo.InvariantCulture);
        var maxLat = double.Parse(parts[2].Trim(), System.Globalization.CultureInfo.InvariantCulture);
        var maxLon = double.Parse(parts[3].Trim(), System.Globalization.CultureInfo.InvariantCulture);

        return new GeoBounds
        {
            MinLatitude = new Latitude(minLat),
            MinLongitude = new Longitude(minLon),
            MaxLatitude = new Latitude(maxLat),
            MaxLongitude = new Longitude(maxLon)
        };
    }

    /// <summary>
    /// Attempts to parse bounds from a comma-separated string.
    /// </summary>
    public static bool TryParse(string text, out GeoBounds bounds)
    {
        bounds = default!;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var parts = text.Split(',');
        if (parts.Length != 4)
            return false;

        if (!double.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var minLat))
            return false;
        if (!double.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var minLon))
            return false;
        if (!double.TryParse(parts[2].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var maxLat))
            return false;
        if (!double.TryParse(parts[3].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var maxLon))
            return false;

        bounds = new GeoBounds
        {
            MinLatitude = new Latitude(minLat),
            MinLongitude = new Longitude(minLon),
            MaxLatitude = new Latitude(maxLat),
            MaxLongitude = new Longitude(maxLon)
        };
        return true;
    }

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
        return HeightDegrees * 111320.0;
    }

    public void Validate()
    {
        if (MaxLatitude.Degrees < MinLatitude.Degrees)
            throw new InvalidOperationException("MaxLatitude must be >= MinLatitude.");
        if (MaxLongitude.Degrees < MinLongitude.Degrees)
            throw new InvalidOperationException("MaxLongitude must be >= MinLongitude.");
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
