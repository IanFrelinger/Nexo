namespace Nexo.GameDomain.Aesthetics;

/// <summary>
/// Recommended logical <see cref="EngineRenderingSurfaceBinding.Role"/> values for cross-engine packs.
/// Hosts may use additional custom roles; unknown roles are not rejected by validation.
/// </summary>
public static class RenderingSurfaceRoles
{
    public const string WorldPrimary = "world_primary";
    public const string CharacterSkin = "character_skin";
    public const string UiDefault = "ui_default";
    public const string EffectsParticles = "effects_particles";

    /// <summary>Roles documented for interoperability; not exhaustive.</summary>
    public static IReadOnlySet<string> Documented { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        WorldPrimary, CharacterSkin, UiDefault, EffectsParticles
    };
}
