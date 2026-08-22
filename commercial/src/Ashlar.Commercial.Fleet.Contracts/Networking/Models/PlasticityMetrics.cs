namespace Ashlar.Commercial.Fleet.Contracts.Networking.Models;
/// <summary>
/// Aggregated metrics for the plasticity (networking/usage/cache) subsystem.
/// </summary>
public sealed record PlasticityMetrics
{
    /// <summary>Node identifier.</summary>
    public string NodeId { get; init; } = "";

    /// <summary>Number of connected peers (from network bus).</summary>
    public int PeerCount { get; init; }

    /// <summary>Number of agents known in the directory (local + discovered).</summary>
    public int AgentCount { get; init; }

    /// <summary>Number of hot bricks (top-N by usage).</summary>
    public int HotBrickCount { get; init; }

    /// <summary>Number of cold bricks (unused in window).</summary>
    public int ColdBrickCount { get; init; }

    /// <summary>Adaptive cache hit rate (0.0–1.0), or null if cache not used.</summary>
    public double? CacheHitRate { get; init; }

    /// <summary>Adaptive cache entry count, or null if cache not used.</summary>
    public int? CacheEntries { get; init; }

    /// <summary>Adaptive cache eviction count, or null if cache not used.</summary>
    public long? CacheEvictionCount { get; init; }

    /// <summary>Knowledge sync: last sync time per peer (peer id -> ISO8601).</summary>
    public IReadOnlyDictionary<string, string> LastKnowledgeSyncByPeer { get; init; } = new Dictionary<string, string>();
}
