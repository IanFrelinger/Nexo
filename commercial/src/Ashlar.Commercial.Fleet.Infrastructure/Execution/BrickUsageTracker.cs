using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ashlar.Commercial.Fleet.Contracts.Networking.Models;
using Ashlar.Commercial.Fleet.Contracts.Networking.Ports;

namespace Ashlar.Commercial.Fleet.Infrastructure.Execution;
/// <summary>
/// In-memory usage tracker for brick executions. Implements synaptic plasticity:
/// hot bricks (frequently used) and cold bricks (unused) for adaptive caching.
/// Thread-safe; uses a rolling window of records.
/// </summary>
public sealed class BrickUsageTracker : IBrickUsageTracker
{
    private readonly BrickUsageTrackerOptions _options;
    private readonly ILogger<BrickUsageTracker>? _logger;
    private readonly List<BrickUsageRecord> _records = new();
    private readonly object _lock = new();

    public BrickUsageTracker(
        IOptions<BrickUsageTrackerOptions> options,
        ILogger<BrickUsageTracker>? logger = null)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }

    /// <inheritdoc />
    public void RecordExecution(BrickUsageRecord record)
    {
        if (record == null) throw new ArgumentNullException(nameof(record));

        lock (_lock)
        {
            _records.Add(record);
            while (_records.Count > _options.MaxEntries)
            {
                _records.RemoveAt(0);
            }
        }
    }

    /// <inheritdoc />
    public BrickUsageStats? GetUsageStats(string brickId)
    {
        if (string.IsNullOrEmpty(brickId)) return null;

        BrickUsageRecord[] snapshot;
        lock (_lock)
        {
            snapshot = _records.ToArray();
        }

        var list = snapshot.Where(r => string.Equals(r.BrickId, brickId, StringComparison.OrdinalIgnoreCase)).ToList();
        if (list.Count == 0) return null;

        var oneHourAgo = DateTimeOffset.UtcNow.AddSeconds(-_options.RollingHourWindowSeconds);
        var inLastHour = list.Where(r => r.Timestamp >= oneHourAgo).ToList();

        return new BrickUsageStats
        {
            TotalExecutions = list.Count,
            SuccessRate = list.Count > 0 ? (double)list.Count(r => r.Success) / list.Count : 0,
            AvgDurationMs = list.Count > 0 ? list.Average(r => r.DurationMs) : 0,
            LastUsed = list.Max(r => r.Timestamp),
            ExecutionsPerHour = inLastHour.Count / Math.Max(1, _options.RollingHourWindowSeconds / 3600.0)
        };
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetHotBricks(int topN)
    {
        if (topN <= 0) return Array.Empty<string>();

        BrickUsageRecord[] snapshot;
        lock (_lock)
        {
            snapshot = _records.ToArray();
        }

        var oneHourAgo = DateTimeOffset.UtcNow.AddSeconds(-_options.RollingHourWindowSeconds);
        var perBrick = snapshot
            .Where(r => r.Timestamp >= oneHourAgo)
            .GroupBy(r => r.BrickId, StringComparer.OrdinalIgnoreCase)
            .Select(g => (BrickId: g.Key, Count: g.Count()))
            .OrderByDescending(x => x.Count)
            .Take(topN)
            .Select(x => x.BrickId)
            .ToList();

        return perBrick;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetColdBricks(TimeSpan unusedFor)
    {
        BrickUsageRecord[] snapshot;
        lock (_lock)
        {
            snapshot = _records.ToArray();
        }

        var cutoff = DateTimeOffset.UtcNow - unusedFor;
        var lastUsed = snapshot
            .GroupBy(r => r.BrickId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Max(r => r.Timestamp));
        var cold = lastUsed.Where(kv => kv.Value < cutoff).Select(kv => kv.Key).ToList();
        return cold;
    }
}
