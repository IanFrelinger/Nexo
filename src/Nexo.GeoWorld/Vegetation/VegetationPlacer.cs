using Nexo.GeoTerrain;
using Nexo.GeoTerrain.Projection;
using Nexo.GeoVector.Geometry;
using Nexo.GeoVector.Generation;
using Nexo.GeoVector.Models;
using Nexo.GeoVector.Values;
using Nexo.GeoWorld.Placement;
using Local2 = Nexo.GeoVector.Geometry.Vector2;

namespace Nexo.GeoWorld.Vegetation;

public static class VegetationPlacer
{
    public static IReadOnlyList<SceneryInstance> Place(
        ElevationGrid terrain,
        GeoFeatureSet? vegetationPolygons,
        GeoFeatureSet? buildingPolygons,
        GeoFeatureSet? waterPolygons,
        GeoFeatureSet? roadLines,
        VegetationPlacementOptions options,
        ICoordinateProjector? projector = null,
        GeoPoint? localOriginOverride = null)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(terrain);
        ArgumentNullException.ThrowIfNull(options);
#else
        if (terrain is null) throw new ArgumentNullException(nameof(terrain));
        if (options is null) throw new ArgumentNullException(nameof(options));
#endif
        if (options.InstancesPerSquareKm <= 0) return Array.Empty<SceneryInstance>();

        var proj = projector ?? EquirectangularProjector.Instance;

        // Local coordinate frame: X east, Z north. Our 2D projector uses (X east, Y north).
        var origin = localOriginOverride ?? new GeoPoint
        {
            Latitude = new Latitude((terrain.Bounds.MinLatitude.Degrees + terrain.Bounds.MaxLatitude.Degrees) * 0.5),
            Longitude = new Longitude((terrain.Bounds.MinLongitude.Degrees + terrain.Bounds.MaxLongitude.Degrees) * 0.5)
        };

        var includeMasks = BuildIncludeMasks(vegetationPolygons, origin, proj);
        var excludeMasks = new List<IPlacementMask>();

        excludeMasks.AddRange(BuildExcludeMasks(buildingPolygons, origin, proj));
        excludeMasks.AddRange(BuildExcludeMasks(waterPolygons, origin, proj));

        var mask = new MaskEvaluator(includeMasks, excludeMasks);

        // Determine area from grid extents (meters).
        var widthM = (float)((terrain.Width - 1) * terrain.Spacing.MetersX);
        var heightM = (float)((terrain.Height - 1) * terrain.Spacing.MetersY);
        if (widthM <= 0 || heightM <= 0) return Array.Empty<SceneryInstance>();

        var areaSqKm = (widthM * heightM) / 1_000_000f;
        var targetCount = (int)Math.Round(areaSqKm * options.InstancesPerSquareKm);
        if (targetCount <= 0) return Array.Empty<SceneryInstance>();
        if (options.MaxInstances > 0 && targetCount > options.MaxInstances) targetCount = options.MaxInstances;

        var rng = new XorShift32((uint)options.Seed);
        var placed = new List<SceneryInstance>(targetCount);
        var placed2D = new List<Local2>(targetCount);

        var minSpacing2 = options.MinInstanceSpacingMeters * options.MinInstanceSpacingMeters;
        var maxTries = Math.Max(512, targetCount * 20);
        if (options.MaxAttempts > 0 && maxTries > options.MaxAttempts) maxTries = options.MaxAttempts;

        for (var attempt = 0; attempt < maxTries && placed.Count < targetCount; attempt++)
        {
            var x = rng.NextFloat01() * widthM;
            var z = rng.NextFloat01() * heightM;
            var p2 = new Local2(x, z);

            if (!mask.IsAllowed(p2)) continue;
            if (!RespectDistances(p2, placed2D, minSpacing2)) continue;
            if (!RespectRoadDistance(p2, roadLines, origin, proj, options.MinDistanceToRoadMeters)) continue;
            if (!RespectPolygonDistance(p2, buildingPolygons, origin, proj, options.MinDistanceToBuildingMeters)) continue;
            if (!RespectPolygonDistance(p2, waterPolygons, origin, proj, options.MinDistanceToWaterMeters)) continue;
            if (!RespectSlope(terrain, x, z, options)) continue;

            if (!ElevationGridSampler.TrySampleHeightMetersAtLocalXZ(terrain, x, z, treatNoDataAsZero: true, out var h))
            {
                h = 0f;
            }

            var yaw = rng.NextFloat01() * 360f;
            var (kind, scale) = PickSpecies(options, ref rng);

            placed.Add(new SceneryInstance
            {
                Kind = kind,
                Position = new Vector3(x, h, z),
                YawDegrees = yaw,
                UniformScale = scale
            });
            placed2D.Add(p2);
        }

        return placed;
    }

    private static bool RespectDistances(Local2 p, List<Local2> existing, float minDistSquared)
    {
        for (var i = 0; i < existing.Count; i++)
        {
            var dx = p.X - existing[i].X;
            var dy = p.Y - existing[i].Y;
            if ((dx * dx + dy * dy) < minDistSquared) return false;
        }
        return true;
    }

    private static bool RespectSlope(ElevationGrid grid, float x, float z, VegetationPlacementOptions options)
    {
        if (!options.MaxSlopeDegrees.HasValue) return true;

        var stepX = (float)grid.Spacing.MetersX;
        var stepZ = (float)grid.Spacing.MetersY;
        if (stepX <= 0 || stepZ <= 0) return true;

        var x1 = Math.Min(x + stepX, (float)((grid.Width - 1) * grid.Spacing.MetersX));
        var z1 = Math.Min(z + stepZ, (float)((grid.Height - 1) * grid.Spacing.MetersY));

        if (!ElevationGridSampler.TrySampleHeightMetersAtLocalXZ(grid, x, z, treatNoDataAsZero: true, out var h00)) return true;
        if (!ElevationGridSampler.TrySampleHeightMetersAtLocalXZ(grid, x1, z, treatNoDataAsZero: true, out var h10)) return true;
        if (!ElevationGridSampler.TrySampleHeightMetersAtLocalXZ(grid, x, z1, treatNoDataAsZero: true, out var h01)) return true;

        var dhdx = (h10 - h00) / (x1 - x + 1e-6f);
        var dhdz = (h01 - h00) / (z1 - z + 1e-6f);
        var slopeRad = (float)Math.Atan(Math.Sqrt(dhdx * dhdx + dhdz * dhdz));
        var slopeDeg = slopeRad * (180f / (float)Math.PI);
        return slopeDeg <= options.MaxSlopeDegrees.Value;
    }

    private static IReadOnlyList<IPlacementMask> BuildIncludeMasks(GeoFeatureSet? set, GeoPoint origin, ICoordinateProjector projector)
    {
        if (set == null || set.Features.Count == 0) return Array.Empty<IPlacementMask>();
        var masks = new List<IPlacementMask>();

        foreach (var f in set.Features)
        {
            if (!f.Kind.Equals(FeatureKind.Vegetation)) continue;
            if (f.Geometry is not GeoPolygon p) continue;
            var ringLocal = GeoProjector.ProjectRingToLocalMeters(p.OuterRing, origin, projector);
            masks.Add(new PolygonMask(ringLocal));
        }

        return masks;
    }

    private static IReadOnlyList<IPlacementMask> BuildExcludeMasks(GeoFeatureSet? set, GeoPoint origin, ICoordinateProjector projector)
    {
        if (set == null || set.Features.Count == 0) return Array.Empty<IPlacementMask>();
        var masks = new List<IPlacementMask>();

        foreach (var f in set.Features)
        {
            if (f.Geometry is not GeoPolygon p) continue;
            var ringLocal = GeoProjector.ProjectRingToLocalMeters(p.OuterRing, origin, projector);
            masks.Add(new PolygonMask(ringLocal));
        }

        return masks;
    }

    private static bool RespectRoadDistance(Local2 p, GeoFeatureSet? roads, GeoPoint origin, ICoordinateProjector projector, float minDistMeters)
    {
        if (minDistMeters <= 0) return true;
        if (roads == null || roads.Features.Count == 0) return true;

        foreach (var f in roads.Features)
        {
            if (!f.Kind.Equals(FeatureKind.Road)) continue;
            if (f.Geometry is not GeoPolyline line) continue;
            var local = GeoProjector.ProjectRingToLocalMeters(line.Points, origin, projector);
            for (var i = 1; i < local.Length; i++)
            {
                var d = Geometry2D.DistancePointToSegment(p, local[i - 1], local[i]);
                if (d < minDistMeters) return false;
            }
        }

        return true;
    }

    private static bool RespectPolygonDistance(Local2 p, GeoFeatureSet? polys, GeoPoint origin, ICoordinateProjector projector, float minDistMeters)
    {
        if (minDistMeters <= 0) return true;
        if (polys == null || polys.Features.Count == 0) return true;

        foreach (var f in polys.Features)
        {
            if (f.Geometry is not GeoPolygon poly) continue;
            var ringLocal = GeoProjector.ProjectRingToLocalMeters(poly.OuterRing, origin, projector);
            if (Geometry2D.PointInPolygon(p, ringLocal)) return false;

            for (var i = 1; i < ringLocal.Length; i++)
            {
                var d = Geometry2D.DistancePointToSegment(p, ringLocal[i - 1], ringLocal[i]);
                if (d < minDistMeters) return false;
            }
        }

        return true;
    }

    private static (string kind, float scale) PickSpecies(VegetationPlacementOptions options, ref XorShift32 rng)
    {
        var species = options.Species;
        if (species == null || species.Count == 0)
        {
            var s = options.MinUniformScale + (options.MaxUniformScale - options.MinUniformScale) * rng.NextFloat01();
            return (options.Kind, s);
        }

        float total = 0;
        for (var i = 0; i < species.Count; i++)
        {
            var w = species[i].Weight;
            if (w > 0) total += w;
        }
        if (total <= 0)
        {
            var s = options.MinUniformScale + (options.MaxUniformScale - options.MinUniformScale) * rng.NextFloat01();
            return (options.Kind, s);
        }

        var r = rng.NextFloat01() * total;
        for (var i = 0; i < species.Count; i++)
        {
            var sp = species[i];
            var w = sp.Weight;
            if (w <= 0) continue;
            r -= w;
            if (r <= 0)
            {
                var kind = string.IsNullOrWhiteSpace(sp.Kind) ? options.Kind : sp.Kind;
                var s = sp.MinUniformScale + (sp.MaxUniformScale - sp.MinUniformScale) * rng.NextFloat01();
                return (kind, s);
            }
        }

        // fallback last
        var last = species[species.Count - 1];
        var lastKind = string.IsNullOrWhiteSpace(last.Kind) ? options.Kind : last.Kind;
        var lastScale = last.MinUniformScale + (last.MaxUniformScale - last.MinUniformScale) * rng.NextFloat01();
        return (lastKind, lastScale);
    }

    private struct XorShift32
    {
        private uint _x;

        public XorShift32(uint seed)
        {
            _x = seed == 0 ? 2463534242u : seed;
        }

        public float NextFloat01()
        {
            // xorshift32
            var x = _x;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _x = x;

            // 24-bit mantissa -> [0,1)
            var v = x >> 8;
            return v * (1.0f / 16777216.0f);
        }
    }
}

