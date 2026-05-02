namespace Nexo.GameDomain.Aesthetics;

/// <summary>
/// Describes the visual style applied to a session, controlling geometry strategy, colour
/// palette, LOD configuration, and post-processing effects.
/// <para>
/// The Unity rendering pipeline reads the active <see cref="AestheticPack"/> to select
/// mesh generators, material shaders, and camera post-process volumes at scene load time.
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
    /// How geographic / OpenStreetMap-style source data is turned into renderable map content.
    /// Use <see cref="MapRenderingProfiles"/> constants (e.g. <c>"voxel_grid"</c>, <c>"vector_overlay"</c>).
    /// <c>"auto"</c> lets the host infer a pipeline from <see cref="GeometryStrategy"/>.
    /// Independent from geometry strategy so you can pair e.g. PBR shading with vector-overlay map features.
    /// </summary>
    public string MapRenderingProfile { get; init; } = MapRenderingProfiles.Auto;

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
}

/// <summary>
/// A single level-of-detail tier within an <see cref="AestheticPack"/>.
/// </summary>
/// <param name="Level">
/// Zero-based LOD index. <c>0</c> is the highest-detail tier rendered at close range.
/// </param>
/// <param name="DetailFactor">
/// Normalised detail multiplier in <c>[0, 1]</c>.
/// Interpretation varies by geometry strategy:
/// <list type="bullet">
///   <item><c>voxel</c> — voxel resolution relative to max grid density.</item>
///   <item><c>low_poly</c> — triangle budget as a fraction of the base mesh.</item>
///   <item><c>pixel_art</c> — sprite resolution multiplier.</item>
///   <item><c>pbr</c> — texture mip level / mesh decimation factor.</item>
///   <item><c>wireframe</c> — edge density ratio.</item>
///   <item><c>sketch</c> — stroke density / hatching frequency.</item>
/// </list>
/// For map-backed scenes, combine tiers with <see cref="AestheticPack.MapRenderingProfile"/> so LOD steps
/// can switch voxel resolution, overlay density, or terrain mesh decimation consistently.
/// </param>
public sealed record LodLevel(
    int Level = 0,
    double DetailFactor = 1.0);
