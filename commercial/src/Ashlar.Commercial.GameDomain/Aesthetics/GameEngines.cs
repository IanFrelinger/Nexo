namespace Ashlar.Commercial.GameDomain.Aesthetics;
/// <summary>
/// Known host identifiers for <see cref="EngineRenderingSurfaceBinding.EngineId"/>.
/// Hosts may use additional custom ids; these are interoperable defaults.
/// </summary>
public static class GameEngines
{
    /// <summary>Constant value for unity.</summary>
    public const string Unity = "unity";
    /// <summary>Constant value for unreal.</summary>
    public const string Unreal = "unreal";
    /// <summary>Constant value for godot.</summary>
    public const string Godot = "godot";
    /// <summary>Constant value for custom.</summary>
    public const string Custom = "custom";
}
