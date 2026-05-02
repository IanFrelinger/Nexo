namespace Nexo.GameDomain.Assets;

/// <summary>
/// Unity build configuration descriptor. Defines target platform,
/// quality settings, player settings, and build options for
/// producing a playable game executable.
/// </summary>
public sealed record BuildDescriptor
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;

    public string TargetPlatform { get; init; } = "StandaloneWindows64"; // StandaloneWindows64, StandaloneOSX, StandaloneLinux64, WebGL, Android, iOS
    public string OutputPath { get; init; } = "Builds/";
    public bool DevelopmentBuild { get; init; }
    public bool AllowDebugging { get; init; }

    public IReadOnlyList<string> Scenes { get; init; } = Array.Empty<string>(); // Scene paths to include
    public string CompanyName { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public string BundleVersion { get; init; } = "0.1.0";

    public QualityConfig Quality { get; init; } = new();
    public IReadOnlyList<string> ScriptingDefines { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}

public sealed record QualityConfig
{
    public int VSyncCount { get; init; } = 1; // 0=off, 1=every vblank, 2=every other
    public int TargetFrameRate { get; init; } = -1; // -1=platform default
    public string ShadowQuality { get; init; } = "all"; // disable, hard_only, all
    public int ShadowResolution { get; init; } = 2048;
    public double ShadowDistance { get; init; } = 150.0;
    public string AntiAliasing { get; init; } = "4x"; // disabled, 2x, 4x, 8x
    public string TextureQuality { get; init; } = "full"; // full, half, quarter, eighth
    public int MaxLod { get; init; } = 0; // 0=highest, 1, 2, etc.
}
