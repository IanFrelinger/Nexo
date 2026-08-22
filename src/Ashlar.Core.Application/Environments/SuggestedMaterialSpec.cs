namespace Ashlar.Core.Application.Environments;

/// <summary>
/// Suggested material parameters and texture-generation hints for the engine adaptation layer.
/// Maps naturally to <c>Ashlar.Commercial.GameDomain</c> <c>MaterialDescriptor</c> (paths filled after image bake).
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
