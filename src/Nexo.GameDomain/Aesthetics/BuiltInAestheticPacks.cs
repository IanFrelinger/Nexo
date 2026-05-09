namespace Nexo.GameDomain.Aesthetics;

/// <summary>
/// Default Forge aesthetic catalog used by <see cref="Mapping.MapAdaptationPlanner"/> when resolving packs from session state.
/// </summary>
public static class BuiltInAestheticPacks
{
    /// <summary>
    /// Built-in packs shipped with the runtime API (single source of truth for Forge endpoints and planners).
    /// </summary>
    public static IReadOnlyList<AestheticPack> Catalog { get; } =
    [
        new AestheticPack
        {
            Id = "voxel", Name = "Voxel", GeometryStrategy = "voxel",
            MapRenderingProfile = MapRenderingProfiles.VoxelGrid,
            DefaultPaletteColors = ["#4CAF50", "#2196F3", "#FF9800", "#9C27B0"],
            LodLevels = [new LodLevel(0, 1.0), new LodLevel(1, 0.5), new LodLevel(2, 0.25)]
        },
        new AestheticPack
        {
            Id = "low_poly", Name = "Low Poly", GeometryStrategy = "low_poly",
            MapRenderingProfile = MapRenderingProfiles.FlatShadedPolys,
            RenderingPipelineKind = RenderingPipelineKinds.ForwardStylized,
            DefaultPaletteColors = ["#81C784", "#64B5F6", "#FFB74D", "#CE93D8"],
            LodLevels = [new LodLevel(0, 1.0), new LodLevel(1, 0.5)]
        },
        new AestheticPack
        {
            Id = "pixel_art", Name = "Pixel Art", GeometryStrategy = "pixel_art",
            MapRenderingProfile = MapRenderingProfiles.OrthographicTile,
            DefaultPaletteColors = ["#388E3C", "#1976D2", "#F57C00", "#7B1FA2"],
            LodLevels = [new LodLevel(0, 1.0)]
        },
        new AestheticPack
        {
            Id = "pbr", Name = "PBR", GeometryStrategy = "pbr",
            MapRenderingProfile = MapRenderingProfiles.HeightfieldMesh,
            RenderingPipelineKind = RenderingPipelineKinds.ForwardPbr,
            DefaultPaletteColors = ["#E0E0E0", "#BDBDBD", "#9E9E9E"],
            LodLevels =
            [
                new LodLevel(0, 1.0), new LodLevel(1, 0.75), new LodLevel(2, 0.5), new LodLevel(3, 0.25)
            ],
            PostProcessEffects = ["bloom", "ambient_occlusion", "tone_mapping"]
        },
        new AestheticPack
        {
            Id = "wireframe", Name = "Wireframe", GeometryStrategy = "wireframe",
            MapRenderingProfile = MapRenderingProfiles.VectorOverlay,
            DefaultPaletteColors = ["#00E676", "#00B0FF"],
            LodLevels = [new LodLevel(0, 1.0), new LodLevel(1, 0.5)]
        },
        new AestheticPack
        {
            Id = "sketch", Name = "Sketch", GeometryStrategy = "sketch",
            MapRenderingProfile = MapRenderingProfiles.VectorOverlay,
            DefaultPaletteColors = ["#212121", "#FAFAFA"],
            LodLevels = [new LodLevel(0, 1.0)],
            PostProcessEffects = ["vignette", "chromatic_aberration"]
        }
    ];
}
