namespace Nexo.GeoTerrain;

/// <summary>
/// Helpers for selecting SRTM tiles that cover a given geographic bounds.
/// </summary>
public static class SrtmTileCoverage
{
    /// <summary>
    /// Returns all 1x1-degree SRTM tiles that cover <paramref name="bounds"/>.
    /// Tiles are returned in north-to-south, west-to-east order.
    /// </summary>
    public static IReadOnlyList<SrtmTileId> TilesCovering(GeoBounds bounds)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(bounds);
#else
        if (bounds is null) throw new ArgumentNullException(nameof(bounds));
#endif
        bounds.Validate();

        // We intentionally do not support antimeridian wrap yet (see GeoBounds.Validate).
        if (bounds.MaxLongitude.Degrees < bounds.MinLongitude.Degrees)
            throw new InvalidOperationException("Antimeridian bounds are not supported yet.");

        // Example: minLat=0, maxLat=1 should include only tile south=0.
        var minSouthLat = (int)Math.Floor(bounds.MinLatitude.Degrees);
        var maxSouthLat = (int)Math.Ceiling(bounds.MaxLatitude.Degrees) - 1;

        var minWestLon = (int)Math.Floor(bounds.MinLongitude.Degrees);
        var maxWestLon = (int)Math.Ceiling(bounds.MaxLongitude.Degrees) - 1;

        if (maxSouthLat < minSouthLat || maxWestLon < minWestLon)
        {
            return Array.Empty<SrtmTileId>();
        }

        // Clamp to valid SRTM tile ID domains.
        if (minSouthLat < -90) minSouthLat = -90;
        if (maxSouthLat > 89) maxSouthLat = 89;
        if (minWestLon < -180) minWestLon = -180;
        if (maxWestLon > 179) maxWestLon = 179;

        var tiles = new List<SrtmTileId>(capacity: (maxSouthLat - minSouthLat + 1) * (maxWestLon - minWestLon + 1));

        // North -> south (higher south latitude is further north).
        for (var southLat = maxSouthLat; southLat >= minSouthLat; southLat--)
        {
            for (var westLon = minWestLon; westLon <= maxWestLon; westLon++)
            {
                tiles.Add(new SrtmTileId(southLat, westLon));
            }
        }

        return tiles;
    }

    // Intentionally no "Describe" helper here to keep netstandard2.0 compatibility simple.
}

