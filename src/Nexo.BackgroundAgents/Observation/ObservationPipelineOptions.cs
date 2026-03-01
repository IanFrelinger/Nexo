namespace Nexo.BackgroundAgents.Observation;

/// <summary>
/// Configuration options for the observation pipeline.
/// </summary>
public sealed class ObservationPipelineOptions
{
    /// <summary>Directory paths to watch (e.g. src/, .github/, tools/). Relative to RepoRoot.</summary>
    public IReadOnlyList<string> WatchPaths { get; set; } = new[] { "src", ".github", "tools" };

    /// <summary>File filters for file system watcher (e.g. *.cs, *.csproj).</summary>
    public IReadOnlyList<string> FileFilters { get; set; } = new[] { "*" };

    /// <summary>Process names to track (e.g. dotnet, msbuild, nexo).</summary>
    public IReadOnlyList<string> ProcessFilters { get; set; } = new[] { "dotnet", "msbuild", "nexo" };

    /// <summary>Repo root. When null, discovered from cwd (walks up to find solution file).</summary>
    public string? RepoRoot { get; set; }

    /// <summary>Path for the pattern store (LiteDB file).</summary>
    public string StorePath { get; set; } = "nexo-patterns.db";

    /// <summary>Sliding window size for pattern detection (seconds).</summary>
    public int PatternWindowSeconds { get; set; } = 300;

    /// <summary>Minimum edits to same path to emit repeated-edits pattern.</summary>
    public int RepeatedEditThreshold { get; set; } = 3;
}
