using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Ashlar.Commercial.GameDomain.Aesthetics;
/// <summary>
/// Builds a JSON manifest for a specific engine from an <see cref="AestheticPack"/>.
/// </summary>
public static class EngineAestheticManifestBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    /// <summary>
    /// Returns a JSON document suitable for engine importers (filtered bindings, pack summary).
    /// </summary>
    public static string BuildJson(string engineId, AestheticPack pack)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(engineId);
        ArgumentNullException.ThrowIfNull(pack);

        var normalized = engineId.Trim();
        var root = new JsonObject
        {
            ["engineId"] = normalized,
            ["aestheticPackId"] = pack.Id,
            ["name"] = pack.Name,
            ["geometryStrategy"] = pack.GeometryStrategy,
            ["mapRenderingProfile"] = string.IsNullOrWhiteSpace(pack.MapRenderingProfile)
                ? MapRenderingProfiles.Auto
                : pack.MapRenderingProfile,
            ["lodLevels"] = JsonSerializer.SerializeToNode(pack.LodLevels, JsonOptions)!,
            ["defaultPaletteColors"] = JsonSerializer.SerializeToNode(pack.DefaultPaletteColors, JsonOptions)!,
            ["postProcessEffects"] = JsonSerializer.SerializeToNode(pack.PostProcessEffects, JsonOptions)!,
            ["surfaceBindings"] = new JsonArray()
        };

        var arr = (JsonArray)root["surfaceBindings"]!;
        foreach (var b in pack.EngineSurfaceBindings.Where(x =>
                     x.EngineId.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
        {
            var o = new JsonObject
            {
                ["role"] = b.Role,
                ["materialSurfaceId"] = b.MaterialSurfaceId,
                ["assetOrShaderHint"] = b.AssetOrShaderHint ?? ""
            };
            if (b.Parameters is { Count: > 0 })
                o["parameters"] = JsonSerializer.SerializeToNode(b.Parameters, JsonOptions)!;
            arr.Add(o);
        }

        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }
}
