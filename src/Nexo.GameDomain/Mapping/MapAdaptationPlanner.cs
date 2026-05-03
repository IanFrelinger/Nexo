using Nexo.GameDomain.Aesthetics;
using Nexo.GameDomain.Scoping;
using Nexo.GameDomain.Session;

namespace Nexo.GameDomain.Mapping;

/// <summary>
/// Derives the effective map rendering profile and a ordered pipeline stage list from the current session.
/// </summary>
public static class MapAdaptationPlanner
{
    /// <summary>
    /// Resolves <see cref="AestheticPack.MapRenderingProfile"/> when set to <see cref="MapRenderingProfiles.Auto"/>.
    /// </summary>
    public static string ResolveEffectiveProfile(AestheticPack pack)
    {
        if (!string.Equals(pack.MapRenderingProfile, MapRenderingProfiles.Auto, StringComparison.Ordinal))
            return pack.MapRenderingProfile;

        return InferFromGeometryStrategy(pack.GeometryStrategy);
    }

    /// <summary>
    /// Builds an adaptation plan using the server-scoped <c>aesthetic</c> setting and packs on the session,
    /// falling back to <paramref name="builtInCatalog"/> when the applied id is not yet materialised in <see cref="SessionState.AestheticPacks"/>.
    /// </summary>
    public static MapAdaptationPlan BuildPlan(
        SessionState session,
        IReadOnlyList<AestheticPack>? builtInCatalog = null)
    {
        var resolver = new ScopeResolver(session.ScopedSettings);
        var raw = resolver.Resolve("aesthetic", new ScopeContext());
        var aestheticId = raw?.ToString()?.Trim();

        if (string.IsNullOrEmpty(aestheticId))
        {
            return new MapAdaptationPlan(
                ActiveAestheticId: string.Empty,
                GeometryStrategy: "low_poly",
                EffectiveMapRenderingProfile: MapRenderingProfiles.Auto,
                ProfileWasInferred: false,
                PipelineStages: ["configure_session", "apply_aesthetic"],
                Notes: "No aesthetic setting; hosts should use defaults or prompt the user.");
        }

        var pack = session.AestheticPacks.FirstOrDefault(p => p.Id == aestheticId)
                   ?? builtInCatalog?.FirstOrDefault(p => p.Id == aestheticId);

        if (pack is null)
        {
            return new MapAdaptationPlan(
                ActiveAestheticId: aestheticId,
                GeometryStrategy: "low_poly",
                EffectiveMapRenderingProfile: MapRenderingProfiles.Auto,
                ProfileWasInferred: false,
                PipelineStages: ["resolve_aesthetic_pack"],
                Notes: $"Aesthetic '{aestheticId}' is set but no matching pack was found on the session or catalog.");
        }

        var inferred = string.Equals(pack.MapRenderingProfile, MapRenderingProfiles.Auto, StringComparison.Ordinal);
        var effective = inferred ? InferFromGeometryStrategy(pack.GeometryStrategy) : pack.MapRenderingProfile;

        return new MapAdaptationPlan(
            ActiveAestheticId: aestheticId,
            GeometryStrategy: pack.GeometryStrategy,
            EffectiveMapRenderingProfile: effective,
            ProfileWasInferred: inferred,
            PipelineStages: BuildPipelineStages(effective),
            Notes: null);
    }

    internal static string InferFromGeometryStrategy(string geometryStrategy)
    {
        return geometryStrategy.ToLowerInvariant() switch
        {
            "voxel" => MapRenderingProfiles.VoxelGrid,
            "low_poly" => MapRenderingProfiles.FlatShadedPolys,
            "pixel_art" => MapRenderingProfiles.OrthographicTile,
            "pbr" => MapRenderingProfiles.HeightfieldMesh,
            "wireframe" => MapRenderingProfiles.VectorOverlay,
            "sketch" => MapRenderingProfiles.VectorOverlay,
            _ => MapRenderingProfiles.VectorOverlay
        };
    }

    private static IReadOnlyList<string> BuildPipelineStages(string effectiveProfile)
    {
        return effectiveProfile switch
        {
            MapRenderingProfiles.VoxelGrid => new[]
            {
                "fetch_vector",
                "optional_intelligence",
                "verify_topology",
                "voxel_rasterize",
                "emit_chunk_payload"
            },
            MapRenderingProfiles.HeightfieldMesh => new[]
            {
                "fetch_terrain",
                "fetch_vector",
                "optional_intelligence",
                "drape_linear_features",
                "mesh_emit"
            },
            MapRenderingProfiles.FlatShadedPolys => new[]
            {
                "fetch_vector",
                "optional_intelligence",
                "extrude_buildings",
                "mesh_emit"
            },
            MapRenderingProfiles.VectorOverlay => new[]
            {
                "fetch_vector",
                "optional_intelligence",
                "overlay_rasterize"
            },
            MapRenderingProfiles.OrthographicTile => new[]
            {
                "fetch_vector",
                "optional_intelligence",
                "stamp_orthographic_tile"
            },
            MapRenderingProfiles.BillboardFeatures => new[]
            {
                "fetch_vector",
                "optional_intelligence",
                "scatter_billboards"
            },
            _ => new[] { "fetch_vector", "optional_intelligence", "host_defined" }
        };
    }
}
