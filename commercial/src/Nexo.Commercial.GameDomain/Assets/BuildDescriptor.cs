namespace Nexo.Commercial.GameDomain.Assets;
/// <summary>
/// Host build configuration descriptor: target platform, quality settings, player settings,
/// and options for producing a playable build.
/// </summary>
public sealed record BuildDescriptor
{
    /// <summary>id value.</summary>
    public string Id { get; init; } = string.Empty;
    /// <summary>Name value.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Target platform value.</summary>
    public string TargetPlatform { get; init; } = "StandaloneWindows64"; // StandaloneWindows64, StandaloneOSX, StandaloneLinux64, WebGL, Android, iOS
    /// <summary>Output path value.</summary>
    public string OutputPath { get; init; } = "Builds/";
    /// <summary>Development build value.</summary>
    public bool DevelopmentBuild { get; init; }
    /// <summary>Allow debugging value.</summary>
    public bool AllowDebugging { get; init; }

    /// <summary>Scenes value.</summary>
    public IReadOnlyList<string> Scenes { get; init; } = Array.Empty<string>(); // Scene paths to include
    /// <summary>Company name value.</summary>
    public string CompanyName { get; init; } = string.Empty;
    /// <summary>Product name value.</summary>
    public string ProductName { get; init; } = string.Empty;
    /// <summary>Bundle version value.</summary>
    public string BundleVersion { get; init; } = "0.1.0";

    /// <summary>Quality value.</summary>
    public QualityConfig Quality { get; init; } = new();
    /// <summary>Scripting defines value.</summary>
    public IReadOnlyList<string> ScriptingDefines { get; init; } = Array.Empty<string>();
    /// <summary>Tags value.</summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}
