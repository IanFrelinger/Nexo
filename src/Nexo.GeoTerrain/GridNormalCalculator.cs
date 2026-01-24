namespace Nexo.GeoTerrain;

/// <summary>
/// Deterministic normals computed directly from grid heights (stable across chunking).
/// </summary>
public static class GridNormalCalculator
{
    public static Vector3[] ComputeNormals(ElevationGrid grid, float verticalScale, bool treatNoDataAsZero)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(grid);
#else
        if (grid is null) throw new ArgumentNullException(nameof(grid));
#endif
        var w = grid.Width;
        var h = grid.Height;
        var normals = new Vector3[checked(w * h)];

        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var xL = x > 0 ? x - 1 : x;
                var xR = x < (w - 1) ? x + 1 : x;
                var yD = y > 0 ? y - 1 : y;
                var yU = y < (h - 1) ? y + 1 : y;

                var hL = Height(grid, xL, y, treatNoDataAsZero) * verticalScale;
                var hR = Height(grid, xR, y, treatNoDataAsZero) * verticalScale;
                var hD = Height(grid, x, yD, treatNoDataAsZero) * verticalScale;
                var hU = Height(grid, x, yU, treatNoDataAsZero) * verticalScale;

                // Tangents in X and Z directions (meters).
                var dx = (float)((xR - xL) * grid.Spacing.MetersX);
                var dz = (float)((yU - yD) * grid.Spacing.MetersY);
                if (dx == 0f) dx = (float)grid.Spacing.MetersX;
                if (dz == 0f) dz = (float)grid.Spacing.MetersY;

                var tangentX = new Vector3(dx, hR - hL, 0f);
                var tangentZ = new Vector3(0f, hU - hD, dz);

                // Cross to get an up-facing normal.
                var n = Vector3.Cross(tangentZ, tangentX).Normalized();
                normals[(y * w) + x] = n;
            }
        }

        return normals;
    }

    private static float Height(ElevationGrid grid, int x, int y, bool treatNoDataAsZero)
    {
        var v = grid.GetHeightMeters(x, y);
        if (!float.IsNaN(v)) return v;
        if (treatNoDataAsZero) return 0f;
        throw new InvalidOperationException($"NoData/NaN elevation at ({x},{y}).");
    }
}

