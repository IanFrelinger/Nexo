namespace Nexo.Orchestration.Build.Models;

/// <summary>
/// Build metrics and statistics.
/// </summary>
public sealed record BuildMetrics
{
    /// <summary>Size of the build artifact in bytes.</summary>
    public long ArtifactSizeBytes { get; init; }

    /// <summary>Number of scripts compiled.</summary>
    public int ScriptCount { get; init; }

    /// <summary>Number of assets processed.</summary>
    public int AssetCount { get; init; }

    /// <summary>Number of shaders compiled.</summary>
    public int ShaderCount { get; init; }

    /// <summary>Time spent compiling scripts.</summary>
    public TimeSpan CompileTime { get; init; }

    /// <summary>Time spent processing assets.</summary>
    public TimeSpan AssetProcessingTime { get; init; }
}
