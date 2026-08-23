namespace Ashlar.Orchestration.Build.Models;

/// <summary>
/// Build configuration.
/// </summary>
public sealed record BuildConfiguration
{
    /// <summary>Path to the project to build.</summary>
    public required string ProjectPath { get; init; }

    /// <summary>Target platform for the build.</summary>
    public required BuildTarget Target { get; init; }

    /// <summary>Whether to produce a development build.</summary>
    public bool Development { get; init; } = true;

    /// <summary>Whether script debugging symbols are enabled.</summary>
    public bool ScriptDebugging { get; init; } = true;

    /// <summary>Optional output directory override.</summary>
    public string? OutputPath { get; init; }

    /// <summary>Scenes to include in the build.</summary>
    public IReadOnlyList<string> Scenes { get; init; } = Array.Empty<string>();

    /// <summary>Preprocessor defines applied during compilation.</summary>
    public IReadOnlyDictionary<string, string> Defines { get; init; } =
        new Dictionary<string, string>();
}
