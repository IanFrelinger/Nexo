using Nexo.GameDomain.Aesthetics;

namespace Nexo.GameDomain.Materials;

/// <summary>
/// Deterministic material hints from palette, geometry strategy, LODs, and optional vector parse kind (M6 baseline).
/// </summary>
public sealed class HeuristicMaterialIntelligenceService : IMaterialIntelligenceService
{
    /// <inheritdoc />
    public Task<MaterialSuggestionResult> SuggestAsync(
        AestheticPack pack,
        string? vectorParseKind,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pack);
        cancellationToken.ThrowIfCancellationRequested();

        var hints = new List<MaterialSurfaceHint>();
        var palette = pack.DefaultPaletteColors?.Where(c => !string.IsNullOrWhiteSpace(c)).ToList()
                        ?? [];
        var roles = new[] { "terrain_base", "water_surface", "road_major", "building_facade", "vegetation", "sky_fill" };

        for (var i = 0; i < roles.Length; i++)
        {
            var hex = palette.Count > 0 ? palette[i % palette.Count] : "#808080";
            hints.Add(new MaterialSurfaceHint(
                roles[i],
                hex,
                $"From aesthetic palette ({pack.Id}); assign to matching engine surface role."));
        }

        hints.Add(new MaterialSurfaceHint(
            "lod_budget",
            pack.LodLevels.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
            $"Use {pack.LodLevels.Count} LOD tier(s); tie fade distances to DetailFactor."));

        hints.Add(new MaterialSurfaceHint(
            "geometry_strategy",
            pack.GeometryStrategy,
            $"Geometry pipeline: {pack.GeometryStrategy}; align tessellation with mapRenderingProfile."));

        var fmt = vectorParseKind?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(fmt))
        {
            hints.Add(fmt switch
            {
                "mvt" => new MaterialSurfaceHint(
                    "vector_layers",
                    "layer_name_as_key",
                    "MVT: bind Mapbox/source layer names to material variants (roads, water, landuse)."),
                "geojson" => new MaterialSurfaceHint(
                    "geojson_props",
                    "properties[style]",
                    "GeoJSON: drive line width / fill from feature properties when present."),
                "osm_xml" => new MaterialSurfaceHint(
                    "osm_tags",
                    "tags[k=v]",
                    "OSM XML: weight highway/waterway/building tags for stroke width and opacity."),
                _ => new MaterialSurfaceHint(
                    "vector_generic",
                    fmt,
                    "Unknown parse kind; treat as generic vector styling.")
            });
        }

        var summary =
            $"{hints.Count} hint(s) for aesthetic '{pack.Id}' ({pack.MapRenderingProfile}); parseKind={fmt ?? "unspecified"}";
        return Task.FromResult(new MaterialSuggestionResult(summary, hints));
    }
}
