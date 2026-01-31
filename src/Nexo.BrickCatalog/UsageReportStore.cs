using System.Collections.Concurrent;
using Nexo.BrickContracts;

namespace Nexo.BrickCatalog;

/// <summary>
/// In-memory store for usage reports from instances. Merged into catalog responses.
/// </summary>
public sealed class UsageReportStore
{
    private readonly ConcurrentDictionary<string, List<BrickUsageRecordDto>> _byNode = new(StringComparer.OrdinalIgnoreCase);
    private readonly int _maxReportsPerNode;
    private readonly ILogger<UsageReportStore>? _logger;

    public UsageReportStore(int maxReportsPerNode = 10, ILogger<UsageReportStore>? logger = null)
    {
        _maxReportsPerNode = Math.Max(1, maxReportsPerNode);
        _logger = logger;
    }

    /// <summary>Store a usage report from a node (replaces previous report from same node).</summary>
    public void Store(BrickUsageReportDto report)
    {
        if (report?.Records == null) return;
        var nodeId = report.NodeId ?? "unknown";
        _byNode[nodeId] = new List<BrickUsageRecordDto>(report.Records);
        _logger?.LogDebug("Stored usage report from {NodeId} with {Count} records", nodeId, report.Records.Count);
    }

    /// <summary>Get aggregated usage stats by brick id (across all nodes).</summary>
    public BrickUsageStatsDto? GetAggregatedStats(string brickId)
    {
        if (string.IsNullOrEmpty(brickId)) return null;

        var records = new List<BrickUsageRecordDto>();
        foreach (var list in _byNode.Values)
        {
            var match = list.FirstOrDefault(r => string.Equals(r.BrickId, brickId, StringComparison.OrdinalIgnoreCase));
            if (match != null)
                records.Add(match);
        }

        if (records.Count == 0) return null;

        var totalExecutions = records.Sum(r => r.ExecutionCount);
        if (totalExecutions == 0) return null;

        var weightedSuccess = records.Sum(r => r.SuccessRate * r.ExecutionCount);
        var weightedDuration = records.Sum(r => r.AvgDurationMs * r.ExecutionCount);

        return new BrickUsageStatsDto
        {
            TotalExecutions = totalExecutions,
            SuccessRate = weightedSuccess / totalExecutions,
            AvgDurationMs = weightedDuration / totalExecutions,
            LastUsed = records.Max(r => r.LastUsed)
        };
    }
}
