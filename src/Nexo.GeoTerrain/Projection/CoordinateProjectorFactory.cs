namespace Nexo.GeoTerrain.Projection;

public static class CoordinateProjectorFactory
{
    public static ICoordinateProjector Create(string? projection, GeoBounds bounds)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(bounds);
#else
        if (bounds is null) throw new ArgumentNullException(nameof(bounds));
#endif
        var p = (projection ?? "auto").Trim();
        if (string.Equals(p, "local", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(p, "enu", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(p, "equirectangular", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(p, "local_equirectangular", StringComparison.OrdinalIgnoreCase))
        {
            return EquirectangularProjector.Instance;
        }

        if (string.Equals(p, "webmercator", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(p, "mercator", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(p, "epsg:3857", StringComparison.OrdinalIgnoreCase))
        {
            return WebMercatorProjector.Instance;
        }

        if (string.Equals(p, "utm", StringComparison.OrdinalIgnoreCase))
        {
            return UtmProjector.CreateForBounds(bounds);
        }

        if (string.Equals(p, "lambert", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(p, "lambert_conformal_conic", StringComparison.OrdinalIgnoreCase))
        {
            return CreateLambertForBounds(bounds);
        }

        if (string.Equals(p, "albers", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(p, "albers_equal_area", StringComparison.OrdinalIgnoreCase))
        {
            return CreateAlbersForBounds(bounds);
        }

        // auto/default
        bounds.Validate();
        var latSpan = Math.Abs(bounds.MaxLatitude.Degrees - bounds.MinLatitude.Degrees);
        var lonSpan = Math.Abs(bounds.MaxLongitude.Degrees - bounds.MinLongitude.Degrees);
        var span = Math.Max(latSpan, lonSpan);
        return span >= 0.25 ? UtmProjector.CreateForBounds(bounds) : EquirectangularProjector.Instance;
    }

    private static LambertConformalConicProjector CreateLambertForBounds(GeoBounds bounds)
    {
        bounds.Validate();
        var midLat = (bounds.MinLatitude.Degrees + bounds.MaxLatitude.Degrees) * 0.5;
        var midLon = (bounds.MinLongitude.Degrees + bounds.MaxLongitude.Degrees) * 0.5;
        var latSpan = bounds.MaxLatitude.Degrees - bounds.MinLatitude.Degrees;
        
        // Use standard parallels at 1/6 and 5/6 of latitude span
        var firstParallel = bounds.MinLatitude.Degrees + latSpan / 6.0;
        var secondParallel = bounds.MinLatitude.Degrees + 5.0 * latSpan / 6.0;
        
        return new LambertConformalConicProjector(
            firstParallel,
            secondParallel,
            midLon,
            midLat);
    }

    private static AlbersEqualAreaProjector CreateAlbersForBounds(GeoBounds bounds)
    {
        bounds.Validate();
        var midLat = (bounds.MinLatitude.Degrees + bounds.MaxLatitude.Degrees) * 0.5;
        var midLon = (bounds.MinLongitude.Degrees + bounds.MaxLongitude.Degrees) * 0.5;
        var latSpan = bounds.MaxLatitude.Degrees - bounds.MinLatitude.Degrees;
        
        // Use standard parallels at 1/6 and 5/6 of latitude span
        var firstParallel = bounds.MinLatitude.Degrees + latSpan / 6.0;
        var secondParallel = bounds.MinLatitude.Degrees + 5.0 * latSpan / 6.0;
        
        return new AlbersEqualAreaProjector(
            firstParallel,
            secondParallel,
            midLon,
            midLat);
    }
}

