namespace Nexo.GeoTerrain;

/// <summary>
/// Deterministically stitches multiple adjacent SRTM tiles into a single <see cref="ElevationGrid"/>.
/// </summary>
public static class SrtmMosaicBuilder
{
    public static ElevationGrid Build(IReadOnlyDictionary<SrtmTileId, byte[]> hgtBytesByTile)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(hgtBytesByTile);
#else
        if (hgtBytesByTile is null) throw new ArgumentNullException(nameof(hgtBytesByTile));
#endif
        if (hgtBytesByTile.Count == 0) throw new ArgumentException("At least one tile is required.", nameof(hgtBytesByTile));

        // Parse sizes and determine grid layout bounds.
        int? size = null;
        var minSouth = int.MaxValue;
        var maxSouth = int.MinValue;
        var minWest = int.MaxValue;
        var maxWest = int.MinValue;

        foreach (var kvp in hgtBytesByTile)
        {
            var tileId = kvp.Key;
            var bytes = kvp.Value;
            if (bytes is null) throw new ArgumentException($"Tile {tileId} has null bytes.", nameof(hgtBytesByTile));

            var summary = SrtmHgtParser.Analyze(bytes);
            size ??= summary.Size;
            if (summary.Size != size.Value)
                throw new InvalidOperationException($"All tiles must have same sample size. Saw {summary.Size} and {size.Value}.");

            if (tileId.SouthLatitudeDegrees < minSouth) minSouth = tileId.SouthLatitudeDegrees;
            if (tileId.SouthLatitudeDegrees > maxSouth) maxSouth = tileId.SouthLatitudeDegrees;
            if (tileId.WestLongitudeDegrees < minWest) minWest = tileId.WestLongitudeDegrees;
            if (tileId.WestLongitudeDegrees > maxWest) maxWest = tileId.WestLongitudeDegrees;
        }

        var tileSize = size!.Value;
        var step = tileSize - 1; // shared borders

        var tilesX = checked(maxWest - minWest + 1);
        var tilesY = checked(maxSouth - minSouth + 1);

        var width = checked(tilesX * step + 1);
        var height = checked(tilesY * step + 1);

        var heights = new float[checked(width * height)];
        for (var i = 0; i < heights.Length; i++) heights[i] = float.NaN;

        // Overall bounds cover the full set of tiles.
        var bounds = new GeoBounds
        {
            MinLatitude = new Latitude(minSouth),
            MaxLatitude = new Latitude(maxSouth + 1),
            MinLongitude = new Longitude(minWest),
            MaxLongitude = new Longitude(maxWest + 1)
        };
        bounds.Validate();

        // Use the same approximation as other GeoTerrain components.
        var midLatRad = (bounds.MinLatitude.Degrees + bounds.MaxLatitude.Degrees) * 0.5 * (Math.PI / 180.0);
        var metersPerDegLat = 111_320.0;
        var metersPerDegLon = 111_320.0 * Math.Cos(midLatRad);
        var degPerSample = 1.0 / (tileSize - 1);

        var spacing = new GridSpacing(
            metersX: metersPerDegLon * degPerSample,
            metersY: metersPerDegLat * degPerSample);

        // Place tiles. Global y=0 is north edge => use tile rows ordered north->south.
        // northmost tile has highest SouthLatitudeDegrees.
        for (var southLat = maxSouth; southLat >= minSouth; southLat--)
        {
            for (var westLon = minWest; westLon <= maxWest; westLon++)
            {
                var tileId = new SrtmTileId(southLat, westLon);
                if (!hgtBytesByTile.TryGetValue(tileId, out var bytes))
                {
                    // Missing tiles are allowed; leave as NaN.
                    continue;
                }

                var tileHeights = SrtmHgtParser.ParseHeightsMeters(bytes, out var parsedSize);
                if (parsedSize != tileSize)
                    throw new InvalidOperationException($"Unexpected tile size for {tileId}: {parsedSize} != {tileSize}.");

                var tx = westLon - minWest;              // west->east
                var ty = maxSouth - southLat;            // north->south
                var x0 = checked(tx * step);
                var y0 = checked(ty * step);

                for (var ly = 0; ly < tileSize; ly++)
                {
                    for (var lx = 0; lx < tileSize; lx++)
                    {
                        var gx = x0 + lx;
                        var gy = y0 + ly;
                        var gi = checked(gy * width + gx);
                        var ti = checked(ly * tileSize + lx);

                        var v = tileHeights[ti];
                        // If already filled (overlap), keep existing unless it is NaN.
                        if (float.IsNaN(heights[gi]))
                        {
                            heights[gi] = v;
                        }
                    }
                }
            }
        }

        return new ElevationGrid(width, height, bounds, spacing, heights);
    }
}

