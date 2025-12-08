namespace Nexo.Orchestration.Build.Models;

/// <summary>
/// Output from a build execution.
/// </summary>
public sealed record BuildOutput
{
    public required string BuildId { get; init; }
    public required bool Success { get; init; }
    public required BuildTarget Target { get; init; }
    public string? ArtifactPath { get; init; }
    public TimeSpan Duration { get; init; }

    public IReadOnlyList<BuildError> Errors { get; init; } = Array.Empty<BuildError>();
    public IReadOnlyList<BuildWarning> Warnings { get; init; } = Array.Empty<BuildWarning>();

    public BuildMetrics? Metrics { get; init; }
}

/// <summary>
/// Build error information.
/// </summary>
public sealed record BuildError
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public string? File { get; init; }
    public int? Line { get; init; }
    public BuildErrorSeverity Severity { get; init; } = BuildErrorSeverity.Error;
}

/// <summary>
/// Build warning information.
/// </summary>
public sealed record BuildWarning
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public string? File { get; init; }
}

/// <summary>
/// Build metrics and statistics.
/// </summary>
public sealed record BuildMetrics
{
    public long ArtifactSizeBytes { get; init; }
    public int ScriptCount { get; init; }
    public int AssetCount { get; init; }
    public int ShaderCount { get; init; }
    public TimeSpan CompileTime { get; init; }
    public TimeSpan AssetProcessingTime { get; init; }
}

/// <summary>
/// Build target platforms.
/// </summary>
public enum BuildTarget
{
    Windows,
    MacOS,
    Linux,
    iOS,
    Android,
    WebGL,
    PS5,
    XboxSeriesX,
    Switch
}

/// <summary>
/// Build error severity levels.
/// </summary>
public enum BuildErrorSeverity
{
    Warning,
    Error,
    Fatal
}

/// <summary>
/// Build configuration.
/// </summary>
public sealed record BuildConfiguration
{
    public required string ProjectPath { get; init; }
    public required BuildTarget Target { get; init; }
    public bool Development { get; init; } = true;
    public bool ScriptDebugging { get; init; } = true;
    public string? OutputPath { get; init; }
    public IReadOnlyList<string> Scenes { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, string> Defines { get; init; } =
        new Dictionary<string, string>();
}

