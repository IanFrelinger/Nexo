namespace Ashlar.Commercial.Fleet.Contracts.Networking.Models;
/// <summary>
/// Statistics for the adaptive brick cache (hit rate, entries, evictions).
/// </summary>
public sealed record AdaptiveBrickCacheStats
{
    /// <summary>Cache hit rate (0.0–1.0).</summary>
    public double HitRate { get; init; }

    /// <summary>Number of entries currently in the cache.</summary>
    public int Entries { get; init; }

    /// <summary>Total number of evictions since startup.</summary>
    public long EvictionCount { get; init; }
}
