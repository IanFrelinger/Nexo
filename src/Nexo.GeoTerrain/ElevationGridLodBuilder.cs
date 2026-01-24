namespace Nexo.GeoTerrain;

/// <summary>
/// Deterministic utilities for generating lower-resolution LOD grids from an elevation grid.
/// </summary>
public static class ElevationGridLodBuilder
{
    /// <summary>
    /// Builds a lower-resolution grid by aggregating <paramref name="factor"/> x <paramref name="factor"/> source samples.
    /// Spacing is multiplied by <paramref name="factor"/> to preserve world scale in meters.
    /// </summary>
    public static ElevationGrid BuildLod(ElevationGrid source, int factor)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfLessThan(factor, 1, nameof(factor));
#else
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (factor < 1) throw new ArgumentOutOfRangeException(nameof(factor));
#endif

        if (factor == 1) return source;

        var srcW = source.Width;
        var srcH = source.Height;

        // Ensure LOD grid is at least 2x2 to remain meshable.
        var dstW = Math.Max(2, (int)Math.Ceiling(srcW / (double)factor));
        var dstH = Math.Max(2, (int)Math.Ceiling(srcH / (double)factor));

        var dstHeights = new float[checked(dstW * dstH)];
        var ii = 0;
        for (var y = 0; y < dstH; y++)
        {
            for (var x = 0; x < dstW; x++)
            {
                var x0 = x * factor;
                var y0 = y * factor;
                var x1 = Math.Min(srcW, x0 + factor);
                var y1 = Math.Min(srcH, y0 + factor);

                // Average non-NaN samples deterministically.
                var sum = 0.0;
                var count = 0;
                for (var sy = y0; sy < y1; sy++)
                {
                    for (var sx = x0; sx < x1; sx++)
                    {
                        var v = source.GetHeightMeters(sx, sy);
                        if (float.IsNaN(v)) continue;
                        sum += v;
                        count++;
                    }
                }

                dstHeights[ii++] = count == 0 ? float.NaN : (float)(sum / count);
            }
        }

        return new ElevationGrid(
            dstW,
            dstH,
            source.Bounds,
            new GridSpacing(source.Spacing.MetersX * factor, source.Spacing.MetersY * factor),
            dstHeights);
    }
}

