using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Ashlar.Orchestration.RateLimiting;

/// <summary>
/// Rate limit window for tracking requests.
/// </summary>
internal sealed class RateLimitWindow
{
    public required string Key { get; init; }
    public DateTimeOffset StartTime { get; set; }
    private int _requestCount;

    // Lock for thread-safe window reset operations
    public readonly object SyncLock = new();

    public int RequestCount => _requestCount;

    /// <summary>
    /// Atomically increments the request count and returns the new value.
    /// </summary>
    public int IncrementAndGet() => Interlocked.Increment(ref _requestCount);

    /// <summary>
    /// Atomically decrements the request count.
    /// </summary>
    public void Decrement() => Interlocked.Decrement(ref _requestCount);

    /// <summary>
    /// Resets the window for a new time period.
    /// </summary>
    public void Reset(DateTimeOffset newStartTime)
    {
        StartTime = newStartTime;
        Interlocked.Exchange(ref _requestCount, 0);
    }
}
