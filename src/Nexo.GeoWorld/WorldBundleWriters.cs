using System.Text.Json;
using Nexo.GeoTerrain;
using Nexo.GeoWorld.Materials;

namespace Nexo.GeoWorld;

/// <summary>
/// Deterministic JSON writers for world bundle artifacts.
/// Keep output stable for CI diffs and Unity import.
/// </summary>
public static class WorldBundleWriters
{
    private static JsonSerializerOptions CreateOptions()
        => new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

    public static string WriteManifest(WorldBundleManifest manifest)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(manifest);
#else
        if (manifest is null) throw new ArgumentNullException(nameof(manifest));
#endif
        // GeoBounds/GeoPoint are simple POCO-ish records; System.Text.Json can serialize them.
        return JsonSerializer.Serialize(manifest, CreateOptions());
    }

    public static string WriteMaterials(IReadOnlyList<MaterialAssignment> materials)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(materials);
#else
        if (materials is null) throw new ArgumentNullException(nameof(materials));
#endif
        return JsonSerializer.Serialize(materials, CreateOptions());
    }

    public static string WriteInstances(IReadOnlyList<SceneryInstance> instances)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(instances);
#else
        if (instances is null) throw new ArgumentNullException(nameof(instances));
#endif
        // Use the same schema as existing GeoTerrain instances output (kind/units/instances).
        var payload = new
        {
            kind = "sceneryInstances",
            units = "meters",
            instances = instances.Select(i => new
            {
                kind = i.Kind,
                position = new { x = i.Position.X, y = i.Position.Y, z = i.Position.Z },
                yawDegrees = i.YawDegrees,
                uniformScale = i.UniformScale
            }).ToArray()
        };
        return JsonSerializer.Serialize(payload, CreateOptions());
    }
}

