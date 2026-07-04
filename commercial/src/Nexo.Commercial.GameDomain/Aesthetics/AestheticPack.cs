namespace Nexo.Commercial.GameDomain.Aesthetics;
/// <summary>
/// Describes the visual style applied to a session, controlling geometry strategy, colour
/// palette, LOD configuration, and post-processing effects.
/// <para>
/// Game engines (Unity, Unreal, Godot, or custom hosts) read the active <see cref="AestheticPack"/> to select
/// mesh generators, materials, shaders, and camera post-process volumes at scene load time.
/// Use <see cref="AestheticPack.EngineSurfaceBindings"/> to map logical roles to engine-specific surfaces while keeping
/// <see cref="GeometryStrategy"/> and palettes engine-neutral.
/// </para>
/// </summary>
public sealed record AestheticPack
{
    /// <summary>Stable identifier used to reference this aesthetic pack.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Human-readable display name shown in the style picker.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Rendering strategy that determines how geometry is generated and shaded.
    /// Accepted values: <c>"voxel"</c>, <c>"low_poly"</c>, <c>"pixel_art"</c>,
    /// <c>"pbr"</c>, <c>"wireframe"</c>, <c>"sketch"</c>.
    /// </summary>
    public string GeometryStrategy { get; init; } = "low_poly";

    /// <summary>
    /// How geographic data should be processed for rendering (see <see cref="MapRenderingProfiles"/>).
    /// </summary>
    public string MapRenderingProfile { get; init; } = MapRenderingProfiles.Auto;

    /// <summary>
    /// Optional reference to a voxel environment manifest <c>Id</c> when
    /// <see cref="GeometryStrategy"/> is <c>"voxel"</c>. The engine adaptation layer resolves this
    /// to composable LOD tiers (voxel pitch, chunk shape, tile-store layers)—the 3D analogue of a
    /// multi-resolution tile map database.
    /// </summary>
    public string? EnvironmentManifestId { get; init; }

    /// <summary>
    /// Default colour palette as a list of hex colour values (e.g. <c>"#FF5733"</c>).
    /// Used for procedural material assignment when explicit textures are absent.
    /// </summary>
    public IReadOnlyList<string> DefaultPaletteColors { get; init; } = [];

    /// <summary>
    /// Ordered list of LOD (level-of-detail) configurations, from highest to lowest fidelity.
    /// </summary>
    public IReadOnlyList<LodLevel> LodLevels { get; init; } = [];

    /// <summary>
    /// Post-processing effects applied to the camera stack (e.g. <c>"bloom"</c>,
    /// <c>"chromatic_aberration"</c>, <c>"vignette"</c>).
    /// </summary>
    public IReadOnlyList<string> PostProcessEffects { get; init; } = [];

    /// <summary>
    /// Semantic render pipeline hint (e.g. <see cref="RenderingPipelineKinds.ForwardStylized"/>).
    /// Hosts translate this to engine-specific renderer features (URP Forward+, Unreal Forward Shading, etc.).
    /// </summary>
    public string RenderingPipelineKind { get; init; } = RenderingPipelineKinds.Auto;

    /// <summary>
    /// Optional per-engine bindings from logical surface roles to concrete shader/material hints.
    /// Empty means hosts infer surfaces from <see cref="GeometryStrategy"/> alone.
    /// </summary>
    public IReadOnlyList<EngineRenderingSurfaceBinding> EngineSurfaceBindings { get; init; } = [];
}
