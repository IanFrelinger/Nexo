using Nexo.Core.Application.Networking.Models;

namespace Nexo.Core.Application.Networking.Ports;

/// <summary>
/// Tracks brick execution usage for synaptic plasticity (strengthen hot paths, weaken cold).
/// </summary>
public interface IBrickUsageTracker
{
    /// <summary>Record a brick execution (fire-and-forget, thread-safe).</summary>
    void RecordExecution(BrickUsageRecord record);

    /// <summary>Get usage statistics for a brick.</summary>
    BrickUsageStats? GetUsageStats(string brickId);

    /// <summary>Get bricks ordered by usage (hot) in the rolling window.</summary>
    IReadOnlyList<string> GetHotBricks(int topN);

    /// <summary>Get brick ids that have not been executed within the given timespan.</summary>
    IReadOnlyList<string> GetColdBricks(TimeSpan unusedFor);
}
