using System.Text.Json;

namespace Nexo.Abstractions;

/// <summary>
/// Snapshot of the world state at a specific tick.
///
/// Contains:
/// - Current tick number
/// - Dictionary of world state data (key-value pairs)
///
/// Used by agents to observe the current world state and by tools to access state.
/// </summary>
/// <param name="Tick">The current tick number.</param>
/// <param name="Data">Dictionary containing world state data as key-value pairs.</param>
public sealed record WorldSnapshot(int Tick, IReadOnlyDictionary<string, object?> Data)
{
    /// <summary>
    /// Creates a world snapshot for repo-based tool execution (RepoRoot and OutputRoot).
    /// </summary>
    /// <param name="repoRoot">Repository root path.</param>
    /// <param name="outputRoot">Output root path; if null, uses Path.Combine(repoRoot, "out").</param>
    /// <param name="tick">Tick number (default 0).</param>
    public static WorldSnapshot ForRepo(string repoRoot, string? outputRoot = null, int tick = 0)
    {
        var output = outputRoot ?? Path.Combine(repoRoot, "out");
        return new WorldSnapshot(tick, new Dictionary<string, object?>
        {
            ["RepoRoot"] = repoRoot,
            ["OutputRoot"] = output
        });
    }
}
