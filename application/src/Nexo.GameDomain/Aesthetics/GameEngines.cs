namespace Nexo.GameDomain.Aesthetics;

/// <summary>
/// Known host identifiers for <see cref="EngineRenderingSurfaceBinding.EngineId"/>.
/// Hosts may use additional custom ids; these are interoperable defaults.
/// </summary>
public static class GameEngines
{
    public const string Unity = "unity";
    public const string Unreal = "unreal";
    public const string Godot = "godot";
    public const string Custom = "custom";
}
