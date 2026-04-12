using System.Collections.Concurrent;
using Nexo.Abstractions;

namespace Nexo.Policies;

/// <summary>
/// Policy that enforces per-tool cumulative execution time budgets.
/// Tracks elapsed time from <see cref="WorldSnapshot.Data"/> entries keyed
/// as "ToolElapsed:{toolId}" (TimeSpan values) and rejects calls once the
/// cumulative time for any single tool exceeds <c>maxPerTool</c>.
/// </summary>
public sealed class PerfHeadroom : IPolicy
{
    private readonly TimeSpan _maxPerTool;
    private readonly ConcurrentDictionary<string, TimeSpan> _cumulativeTime = new(StringComparer.OrdinalIgnoreCase);

    public PerfHeadroom(TimeSpan maxPerTool) => _maxPerTool = maxPerTool;

    public bool Approve(ToolCall call, WorldSnapshot s, out string reason)
    {
        var elapsedKey = $"ToolElapsed:{call.Id}";
        if (s.Data.TryGetValue(elapsedKey, out var elapsedObj) && elapsedObj is TimeSpan elapsed)
        {
            var cumulative = _cumulativeTime.AddOrUpdate(call.Id, elapsed, (_, existing) => existing + elapsed);
            if (cumulative > _maxPerTool)
            {
                reason = $"Tool '{call.Id}' exceeded time budget: {cumulative.TotalSeconds:F1}s > {_maxPerTool.TotalSeconds:F1}s limit";
                return false;
            }
        }

        reason = "OK";
        return true;
    }

    /// <summary>Resets cumulative time tracking for all tools.</summary>
    public void Reset() => _cumulativeTime.Clear();
}
