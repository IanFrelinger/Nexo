namespace Nexo.GeoTerrain;

/// <summary>
/// Deterministic sampling helpers for <see cref="ElevationGrid"/>.
/// </summary>
public static class ElevationGridSampler
{
    public static bool TrySampleHeightMetersAtLocalXZ(
        ElevationGrid grid,
        float xMeters,
        float zMeters,
        bool treatNoDataAsZero,
        out float heightMeters)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(grid);
#else
        if (grid is null) throw new ArgumentNullException(nameof(grid));
#endif
        heightMeters = 0f;

        var totalWidthMeters = (float)((grid.Width - 1) * grid.Spacing.MetersX);
        var totalHeightMeters = (float)((grid.Height - 1) * grid.Spacing.MetersY);
        if (totalWidthMeters <= 0 || totalHeightMeters <= 0) return false;

        // Clamp within grid extents.
        if (xMeters < 0) xMeters = 0;
        if (zMeters < 0) zMeters = 0;
        if (xMeters > totalWidthMeters) xMeters = totalWidthMeters;
        if (zMeters > totalHeightMeters) zMeters = totalHeightMeters;

        var fx = xMeters / (float)grid.Spacing.MetersX;
        var fz = zMeters / (float)grid.Spacing.MetersY;

        var x0 = (int)Math.Floor(fx);
        var z0 = (int)Math.Floor(fz);
        var x1 = x0 + 1;
        var z1 = z0 + 1;

        if (x0 < 0) x0 = 0;
        if (z0 < 0) z0 = 0;
        if (x1 >= grid.Width) x1 = grid.Width - 1;
        if (z1 >= grid.Height) z1 = grid.Height - 1;

        var tx = fx - x0;
        var tz = fz - z0;

        var h00 = grid.GetHeightMeters(x0, z0);
        var h10 = grid.GetHeightMeters(x1, z0);
        var h01 = grid.GetHeightMeters(x0, z1);
        var h11 = grid.GetHeightMeters(x1, z1);

        if (float.IsNaN(h00) || float.IsNaN(h10) || float.IsNaN(h01) || float.IsNaN(h11))
        {
            if (!treatNoDataAsZero) return false;
            if (float.IsNaN(h00)) h00 = 0f;
            if (float.IsNaN(h10)) h10 = 0f;
            if (float.IsNaN(h01)) h01 = 0f;
            if (float.IsNaN(h11)) h11 = 0f;
        }

        // Bilinear interpolation.
        var a = h00 + (h10 - h00) * tx;
        var b = h01 + (h11 - h01) * tx;
        heightMeters = a + (b - a) * tz;
        return true;
    }

    public static bool TrySampleHeightMetersAtGeoPoint(
        ElevationGrid grid,
        GeoPoint point,
        bool treatNoDataAsZero,
        out float heightMeters)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(point);
#else
        if (grid is null) throw new ArgumentNullException(nameof(grid));
        if (point is null) throw new ArgumentNullException(nameof(point));
#endif
        heightMeters = 0f;

        var b = grid.Bounds;
        b.Validate();

        var widthDeg = b.WidthDegrees;
        var heightDeg = b.HeightDegrees;
        if (widthDeg <= 0 || heightDeg <= 0) return false;

        var totalWidthMeters = (float)((grid.Width - 1) * grid.Spacing.MetersX);
        var totalHeightMeters = (float)((grid.Height - 1) * grid.Spacing.MetersY);

        var lon = point.Longitude.Degrees;
        var lat = point.Latitude.Degrees;

        var tx = (lon - b.MinLongitude.Degrees) / widthDeg;
        var tz = (b.MaxLatitude.Degrees - lat) / heightDeg; // north->south

        if (tx < 0) tx = 0;
        if (tx > 1) tx = 1;
        if (tz < 0) tz = 0;
        if (tz > 1) tz = 1;

        var xMeters = (float)(tx * totalWidthMeters);
        var zMeters = (float)(tz * totalHeightMeters);
        return TrySampleHeightMetersAtLocalXZ(grid, xMeters, zMeters, treatNoDataAsZero, out heightMeters);
    }
}

