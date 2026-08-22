namespace Ashlar.Orchestration.Build.Models;

/// <summary>
/// Output from a build execution.
/// 
/// Contains:
/// - Build identification and success status
/// - Target platform
/// - Artifact path and duration
/// - Errors and warnings
/// - Build metrics (size, counts, compile times)
/// 
/// Used by build agents to return build execution results.
/// </summary>
public sealed record BuildOutput
{
    /// <summary>Unique build identifier.</summary>
    public required string BuildId { get; init; }

    /// <summary>Whether the build completed successfully.</summary>
    public required bool Success { get; init; }

    /// <summary>Target platform that was built.</summary>
    public required BuildTarget Target { get; init; }

    /// <summary>Path to the produced artifact, if any.</summary>
    public string? ArtifactPath { get; init; }

    /// <summary>Total elapsed build duration.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Errors encountered during the build.</summary>
    public IReadOnlyList<BuildError> Errors { get; init; } = Array.Empty<BuildError>();

    /// <summary>Warnings encountered during the build.</summary>
    public IReadOnlyList<BuildWarning> Warnings { get; init; } = Array.Empty<BuildWarning>();

    /// <summary>Build statistics and timing metrics.</summary>
    public BuildMetrics? Metrics { get; init; }
}
