using Nexo.GeoTerrain;
using Nexo.GeoTerrain.Projection;
using Nexo.GeoVector.Generation;
using Nexo.GeoVector.Geometry;
using Nexo.GeoVector.Models;
using Nexo.GeoVector.Values;
using Local2 = Nexo.GeoTerrain.Vector2;

namespace Nexo.GeoWorld.Terrain;

public sealed record WaterTerrainCarverOptions
{
    public bool TreatNoDataAsZero { get; init; } = true;

    /// <summary>
    /// If true, flatten all water polygons.
    /// </summary>
    public bool FlattenWater { get; init; } = true;

    /// <summary>
    /// Extra height offset applied to flattened water (meters).
    /// </summary>
    public float WaterSurfaceOffsetMeters { get; init; }

    /// <summary>
    /// Number of smoothing iterations applied near shorelines.
    /// </summary>
    public int ShorelineSmoothIterations { get; init; } = 2;

    /// <summary>
    /// Blend factor per iteration (0..1) when smoothing shoreline cells.
    /// </summary>
    public float ShorelineSmoothBlend { get; init; } = 0.5f;
}

/// <summary>
/// Deterministic terrain modifier for water bodies: flattens water surfaces and optionally smooths shorelines.
/// </summary>
public static class WaterTerrainCarver
{
    public static ElevationGrid Carve(
        ElevationGrid terrain,
        GeoFeatureSet? water,
        GeoPoint origin,
        ICoordinateProjector projector,
        WaterTerrainCarverOptions options)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(terrain);
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentNullException.ThrowIfNull(projector);
        ArgumentNullException.ThrowIfNull(options);
#else
        if (terrain is null) throw new ArgumentNullException(nameof(terrain));
        if (origin is null) throw new ArgumentNullException(nameof(origin));
        if (projector is null) throw new ArgumentNullException(nameof(projector));
        if (options is null) throw new ArgumentNullException(nameof(options));
#endif
        if (water == null || water.Features.Count == 0) return terrain;
        if (!options.FlattenWater) return terrain;

        // Gather water polygons and project to local meters (Vector2: X east, Y north -> map to X,Z).
        var polys = new List<Local2[]>(capacity: Math.Min(128, water.Features.Count));
        for (var i = 0; i < water.Features.Count; i++)
        {
            var f = water.Features[i];
            if (!f.Kind.Equals(FeatureKind.Water)) continue;
            if (f.Geometry is not GeoPolygon poly) continue;
            if (poly.OuterRing.Count < 3) continue;

            var ringLocal = GeoProjector.ProjectRingToLocalMeters(poly.OuterRing, origin, projector);
            if (ringLocal.Length < 3) continue;
            // Convert GeoVector.Geometry.Vector2 -> local meters (GeoTerrain.Vector2).
            var ring = new Local2[ringLocal.Length];
            for (var k = 0; k < ring.Length; k++) ring[k] = new Local2(ringLocal[k].X, ringLocal[k].Y);
            polys.Add(ring);
        }
        if (polys.Count == 0) return terrain;

        var width = terrain.Width;
        var height = terrain.Height;
        var src = terrain.CloneHeightsMeters();
        var dst = new float[src.Length];
        Array.Copy(src, dst, src.Length);

        // Build water mask per grid vertex.
        var waterMask = new bool[src.Length];

        for (var y = 0; y < height; y++)
        {
            var z = (float)(y * terrain.Spacing.MetersY);
            for (var x = 0; x < width; x++)
            {
                var xx = (float)(x * terrain.Spacing.MetersX);
                var p = new Local2(xx, z);

                var inside = false;
                for (var pi = 0; pi < polys.Count; pi++)
                {
                    if (PointInPolygon(p, polys[pi]))
                    {
                        inside = true;
                        break;
                    }
                }

                if (!inside) continue;
                var idx = y * width + x;
                waterMask[idx] = true;
            }
        }

        // Choose a stable water height: use the minimum sampled terrain height within water cells.
        float minH = float.PositiveInfinity;
        for (var i = 0; i < dst.Length; i++)
        {
            if (!waterMask[i]) continue;
            var h0 = dst[i];
            if (float.IsNaN(h0))
            {
                if (!options.TreatNoDataAsZero) continue;
                h0 = 0f;
            }
            if (h0 < minH) minH = h0;
        }
        if (float.IsPositiveInfinity(minH)) return terrain;
        var waterH = minH + options.WaterSurfaceOffsetMeters;

        // Flatten inside water.
        for (var i = 0; i < dst.Length; i++)
        {
            if (waterMask[i]) dst[i] = waterH;
        }

        // Shoreline smoothing: blend cells adjacent to water toward neighborhood average.
        var iters = Math.Max(0, options.ShorelineSmoothIterations);
        if (iters > 0)
        {
            var blend = Clamp01(options.ShorelineSmoothBlend);
            var tmp = new float[dst.Length];

            for (var iter = 0; iter < iters; iter++)
            {
                Array.Copy(dst, tmp, dst.Length);
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var idx = y * width + x;
                        if (!IsShoreCell(waterMask, width, height, x, y)) continue;

                        var avg = NeighborAverage(tmp, width, height, x, y, options.TreatNoDataAsZero);
                        dst[idx] = Lerp(tmp[idx], avg, blend);
                    }
                }
            }
        }

        return new ElevationGrid(width, height, terrain.Bounds, terrain.Spacing, dst);
    }

    private static bool IsShoreCell(bool[] mask, int w, int h, int x, int y)
    {
        var idx = y * w + x;
        var inside = mask[idx];
        // Shore = inside water OR adjacent to water.
        if (inside) return true;
        for (var dy = -1; dy <= 1; dy++)
        {
            var yy = y + dy;
            if (yy < 0 || yy >= h) continue;
            for (var dx = -1; dx <= 1; dx++)
            {
                var xx = x + dx;
                if (xx < 0 || xx >= w) continue;
                if (mask[yy * w + xx]) return true;
            }
        }
        return false;
    }

    private static float NeighborAverage(float[] heights, int w, int h, int x, int y, bool treatNoDataAsZero)
    {
        float sum = 0;
        int cnt = 0;
        for (var dy = -1; dy <= 1; dy++)
        {
            var yy = y + dy;
            if (yy < 0 || yy >= h) continue;
            for (var dx = -1; dx <= 1; dx++)
            {
                var xx = x + dx;
                if (xx < 0 || xx >= w) continue;
                var v = heights[yy * w + xx];
                if (float.IsNaN(v))
                {
                    if (!treatNoDataAsZero) continue;
                    v = 0f;
                }
                sum += v;
                cnt++;
            }
        }
        return cnt > 0 ? (sum / cnt) : 0f;
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    private static float Clamp01(float v) => v < 0 ? 0 : (v > 1 ? 1 : v);

    private static bool PointInPolygon(Local2 p, Local2[] ring)
    {
        // Ray casting.
        var inside = false;
        for (int i = 0, j = ring.Length - 1; i < ring.Length; j = i++)
        {
            var xi = ring[i].X;
            var yi = ring[i].Y;
            var xj = ring[j].X;
            var yj = ring[j].Y;

            var intersect = ((yi > p.Y) != (yj > p.Y)) &&
                            (p.X < (xj - xi) * (p.Y - yi) / (yj - yi + 1e-12f) + xi);
            if (intersect) inside = !inside;
        }
        return inside;
    }
}

