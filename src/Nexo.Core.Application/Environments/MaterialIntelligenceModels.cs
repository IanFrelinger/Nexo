namespace Nexo.Core.Application.Environments;

/// <summary>
/// Suggested material parameters and texture-generation hints for the engine adaptation layer.
/// Maps naturally to <c>Nexo.Commercial.GameDomain</c> <c>MaterialDescriptor</c> (paths filled after image bake).
/// </summary>
/// <param name="Id">Stable material id.</param>
/// <param name="Name">Display name.</param>
/// <param name="ShaderName">Engine shader (e.g. URP Lit).</param>
/// <param name="ColorHex">Base color #RRGGBB.</param>
/// <param name="Metallic">[0,1]</param>
/// <param name="Smoothness">[0,1]</param>
/// <param name="RenderMode">Opaque, Cutout, Transparent.</param>
/// <param name="TextureSlotHints">Shader slot → generation prompt or style recipe (host runs diffusion/procedural step).</param>
public sealed record SuggestedMaterialSpec(
    string Id,
    string Name,
    string ShaderName,
    string ColorHex,
    double Metallic,
    double Smoothness,
    string RenderMode,
    IReadOnlyDictionary<string, string> TextureSlotHints);

/// <summary>Batch of suggested materials (e.g. facades, roads, water).</summary>
public sealed record MaterialIntelligenceResult(IReadOnlyList<SuggestedMaterialSpec> Materials, string? Summary);

/// <summary>Input for material / texture intelligence.</summary>
/// <param name="StylePreset">High-level style (e.g. mediterranean_facade, industrial_roof).</param>
/// <param name="Tags">Optional tags from OSM or design doc (building:levels, surface=asphalt).</param>
/// <param name="Context">Correlation and hints.</param>
/// <param name="MaxMaterials">Cap on returned suggestions.</param>
public sealed record MaterialIntelligenceRequest(
    string StylePreset,
    IReadOnlyDictionary<string, string> Tags,
    MapDataRequestContext Context,
    int MaxMaterials = 16);
