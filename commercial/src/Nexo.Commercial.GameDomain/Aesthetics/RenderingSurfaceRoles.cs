namespace Nexo.Commercial.GameDomain.Aesthetics;
/// <summary>
/// Recommended logical <see cref="EngineRenderingSurfaceBinding.Role"/> values for cross-engine packs.
/// Hosts may use additional custom roles; unknown roles are not rejected by validation.
/// </summary>
public static class RenderingSurfaceRoles
{
    /// <summary>Constant value for world primary.</summary>
    public const string WorldPrimary = "world_primary";
    /// <summary>Constant value for character skin.</summary>
    public const string CharacterSkin = "character_skin";
    /// <summary>Constant value for ui default.</summary>
    public const string UiDefault = "ui_default";
    /// <summary>Constant value for effects particles.</summary>
    public const string EffectsParticles = "effects_particles";

    /// <summary>Roles documented for interoperability; not exhaustive.</summary>
    public static IReadOnlySet<string> Documented { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        WorldPrimary, CharacterSkin, UiDefault, EffectsParticles
    };
}
