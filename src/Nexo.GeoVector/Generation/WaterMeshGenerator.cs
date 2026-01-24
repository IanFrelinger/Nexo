using Nexo.GeoTerrain;
using Nexo.GeoTerrain.Projection;
using Nexo.GeoVector.Geometry;
using Nexo.GeoVector.Models;
using Nexo.GeoVector.Values;

namespace Nexo.GeoVector.Generation;

/// <summary>
/// Deterministic polygon-to-mesh generator for water surfaces (ear-clipped).
/// </summary>
public static class WaterMeshGenerator
{
    public static MeshData GenerateWater(
        GeoFeatureSet water,
        GeoPoint localOrigin,
        ElevationGrid? terrainGrid,
        WaterMeshGenerationOptions options,
        ICoordinateProjector? projector = null)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(water);
        ArgumentNullException.ThrowIfNull(localOrigin);
        ArgumentNullException.ThrowIfNull(options);
#else
        if (water is null) throw new ArgumentNullException(nameof(water));
        if (localOrigin is null) throw new ArgumentNullException(nameof(localOrigin));
        if (options is null) throw new ArgumentNullException(nameof(options));
#endif
        if (options.GenerateTexCoords && options.UvMetersPerRepeat <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), options.UvMetersPerRepeat, "UvMetersPerRepeat must be > 0.");

        var v = new List<Vector3>(capacity: 1024);
        var n = new List<Vector3>(capacity: 1024);
        var uv = options.GenerateTexCoords ? new List<Nexo.GeoTerrain.Vector2>(capacity: 1024) : null;
        var tris = new List<int>(capacity: 2048);

        foreach (var f in water.Features)
        {
            if (!f.Kind.Equals(FeatureKind.Water)) continue;
            if (f.Geometry is not GeoPolygon poly) continue;
            if (poly.OuterRing.Count < 3) continue;

            var ringLocal = GeoProjector.ProjectRingToLocalMeters(poly.OuterRing, localOrigin, projector ?? EquirectangularProjector.Instance);
            if (ringLocal.Length < 3) continue;

            // Remove duplicate closing point for triangulation if present.
            var count = ringLocal.Length;
            if (count >= 2 && ringLocal[0].X == ringLocal[count - 1].X && ringLocal[0].Y == ringLocal[count - 1].Y)
            {
                count -= 1;
            }
            if (count < 3) continue;

            var poly2 = new Nexo.GeoVector.Geometry.Vector2[count];
            for (var i = 0; i < count; i++) poly2[i] = ringLocal[i];

            var tri = EarClipTriangulator.Triangulate(poly2);
            if (tri.Count == 0) continue;

            var start = v.Count;
            float? flattenedY = null;
            if (terrainGrid != null && options.FlattenToTerrain)
            {
                // Sample height at polygon centroid in geo coordinates.
                double latSum = 0;
                double lonSum = 0;
                var cnt = 0;
                for (var i = 0; i < poly.OuterRing.Count; i++)
                {
                    var gp = poly.OuterRing[i];
                    latSum += gp.Latitude.Degrees;
                    lonSum += gp.Longitude.Degrees;
                    cnt++;
                }

                if (cnt > 0)
                {
                    var centroid = new GeoPoint
                    {
                        Latitude = new Latitude(latSum / cnt),
                        Longitude = new Longitude(lonSum / cnt)
                    };

                    if (ElevationGridSampler.TrySampleHeightMetersAtGeoPoint(terrainGrid, centroid, options.TreatNoDataAsZero, out var h))
                    {
                        flattenedY = h + options.SurfaceOffsetMeters;
                    }
                }
            }

            for (var i = 0; i < count; i++)
            {
                var gp = poly.OuterRing[Math.Min(i, poly.OuterRing.Count - 1)];
                var y = 0f;
                if (flattenedY.HasValue)
                {
                    y = flattenedY.Value;
                }
                else if (terrainGrid != null && options.ConformToTerrain)
                {
                    if (ElevationGridSampler.TrySampleHeightMetersAtGeoPoint(terrainGrid, gp, options.TreatNoDataAsZero, out var h))
                        y = h + options.SurfaceOffsetMeters;
                }

                var p = poly2[i];
                v.Add(new Vector3(p.X, y, p.Y));
                n.Add(new Vector3(0, 1, 0));
                if (uv != null)
                {
                    uv.Add(new Nexo.GeoTerrain.Vector2(p.X / options.UvMetersPerRepeat, p.Y / options.UvMetersPerRepeat));
                }
            }

            for (var i = 0; i < tri.Count; i += 3)
            {
                tris.Add(start + tri[i]);
                tris.Add(start + tri[i + 1]);
                tris.Add(start + tri[i + 2]);
            }
        }

        return new MeshData
        {
            Vertices = v,
            Indices = tris,
            Normals = n,
            TexCoords = uv
        };
    }
}

