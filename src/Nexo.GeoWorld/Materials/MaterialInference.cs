using Nexo.GeoVector.Models;

namespace Nexo.GeoWorld.Materials;

public static class MaterialInference
{
    public static IReadOnlyList<MaterialAssignment> InferForBuildings(GeoFeatureSet buildings, float? uvMetersPerRepeat = null, string? baseColorTexturePath = null)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(buildings);
#else
        if (buildings is null) throw new ArgumentNullException(nameof(buildings));
#endif
        var material = "building_default";

        // Very small deterministic heuristic from tags.
        var f = buildings.Features.Count > 0 ? buildings.Features[0] : null;
        if (f?.Properties != null)
        {
            if (TryGetString(f.Properties, "building", out var b))
            {
                if (ContainsAny(b, "industrial", "warehouse")) material = "building_industrial";
                else if (ContainsAny(b, "commercial", "retail")) material = "building_commercial";
                else material = "building_residential";
            }
        }

        return new[]
        {
            new MaterialAssignment
            {
                MeshKind = "buildings",
                MaterialName = material,
                UvMetersPerRepeat = uvMetersPerRepeat,
                BaseColorTexturePath = baseColorTexturePath
            }
        };
    }

    public static IReadOnlyList<MaterialAssignment> InferForRoads(GeoFeatureSet roads, float? uvMetersPerRepeat = null, string? baseColorTexturePath = null)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(roads);
#else
        if (roads is null) throw new ArgumentNullException(nameof(roads));
#endif
        var material = "road_asphalt";

        var f = roads.Features.Count > 0 ? roads.Features[0] : null;
        if (f?.Properties != null)
        {
            if (TryGetString(f.Properties, "class", out var c) || TryGetString(f.Properties, "highway", out c))
            {
                if (ContainsAny(c, "track", "path", "footway")) material = "road_dirt";
                else if (ContainsAny(c, "service")) material = "road_concrete";
            }
        }

        return new[]
        {
            new MaterialAssignment
            {
                MeshKind = "roads",
                MaterialName = material,
                UvMetersPerRepeat = uvMetersPerRepeat,
                BaseColorTexturePath = baseColorTexturePath
            }
        };
    }

    public static IReadOnlyList<MaterialAssignment> InferForWater(float? uvMetersPerRepeat = null, string? baseColorTexturePath = null)
        => new[]
        {
            new MaterialAssignment
            {
                MeshKind = "water",
                MaterialName = "water",
                UvMetersPerRepeat = uvMetersPerRepeat,
                BaseColorTexturePath = baseColorTexturePath
            }
        };

    public static IReadOnlyList<MaterialAssignment> InferForTerrain(float? uvMetersPerRepeat = null, string? baseColorTexturePath = null)
        => new[]
        {
            new MaterialAssignment
            {
                MeshKind = "terrain",
                MaterialName = "terrain",
                UvMetersPerRepeat = uvMetersPerRepeat,
                BaseColorTexturePath = baseColorTexturePath
            }
        };

    private static bool TryGetString(IReadOnlyDictionary<string, object> props, string key, out string value)
    {
        if (props.TryGetValue(key, out var obj) && obj != null)
        {
            value = Convert.ToString(obj, System.Globalization.CultureInfo.InvariantCulture) ?? "";
            return value.Length > 0;
        }
        value = "";
        return false;
    }

    private static bool ContainsAny(string text, params string[] needles)
    {
        for (var i = 0; i < needles.Length; i++)
        {
#if NET8_0_OR_GREATER
            if (text.Contains(needles[i], StringComparison.OrdinalIgnoreCase)) return true;
#else
            if (text.IndexOf(needles[i], StringComparison.OrdinalIgnoreCase) >= 0) return true;
#endif
        }
        return false;
    }
}

