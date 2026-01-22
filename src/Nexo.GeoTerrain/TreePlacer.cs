namespace Nexo.GeoTerrain;

/// <summary>
/// Deterministic tree placement for a given grid and bounds.
/// This is a "good enough" baseline until landcover/water/roads masks are added.
/// </summary>
public static class TreePlacer
{
    public static IReadOnlyList<SceneryInstance> PlaceTrees(ElevationGrid grid, GeoBounds bounds, TreePlacementOptions? options = null)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(bounds);
#else
        if (grid is null) throw new ArgumentNullException(nameof(grid));
        if (bounds is null) throw new ArgumentNullException(nameof(bounds));
#endif
        options ??= new TreePlacementOptions();
        bounds.Validate();

        if (options.TreesPerSquareKm < 0) throw new ArgumentOutOfRangeException(nameof(options), options.TreesPerSquareKm, "TreesPerSquareKm must be >= 0.");
        if (options.MinUniformScale <= 0 || options.MaxUniformScale <= 0) throw new ArgumentOutOfRangeException(nameof(options), "Scale must be > 0.");
        if (options.MaxUniformScale < options.MinUniformScale) throw new InvalidOperationException("MaxUniformScale must be >= MinUniformScale.");

        // Compute approximate area in meters^2 from grid spacing/extents.
        var widthMeters = (float)((grid.Width - 1) * grid.Spacing.MetersX);
        var heightMeters = (float)((grid.Height - 1) * grid.Spacing.MetersY);
        var areaM2 = widthMeters * heightMeters;
        if (areaM2 <= 0) return Array.Empty<SceneryInstance>();

        var areaSqKm = areaM2 / 1_000_000f;
        var count = (int)Math.Round(options.TreesPerSquareKm * areaSqKm);
        if (count <= 0) return Array.Empty<SceneryInstance>();

        var rng = new XorShift32((uint)options.Seed);
        var instances = new List<SceneryInstance>(count);

        for (var i = 0; i < count; i++)
        {
            // Uniform in local meters box; later we can use masks (landcover/water).
            var x = rng.NextFloat01() * widthMeters;
            var z = rng.NextFloat01() * heightMeters;

            if (!ElevationGridSampler.TrySampleHeightMetersAtLocalXZ(grid, x, z, options.TreatNoDataAsZero, out var y))
            {
                continue;
            }

            var yaw = rng.NextFloat01() * 360.0f;
            var scale = Lerp(options.MinUniformScale, options.MaxUniformScale, rng.NextFloat01());

            instances.Add(new SceneryInstance
            {
                Kind = "tree",
                Position = new Vector3(x, y, z),
                YawDegrees = yaw,
                UniformScale = scale
            });
        }

        return instances;
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    private struct XorShift32
    {
        private uint _x;

        public XorShift32(uint seed)
        {
            _x = seed == 0 ? 2463534242u : seed;
        }

        public uint NextU32()
        {
            // xorshift32
            var x = _x;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _x = x;
            return x;
        }

        public float NextFloat01()
        {
            // 24-bit mantissa -> [0,1)
            var v = NextU32() >> 8;
            return v * (1.0f / 16777216.0f);
        }
    }
}

