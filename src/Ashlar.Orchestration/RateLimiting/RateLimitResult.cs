using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Ashlar.Orchestration.RateLimiting;

/// <summary>
/// Result of a rate limit check.
/// </summary>
public sealed record RateLimitResult
{
    /// <summary>Whether the request is allowed under the limit.</summary>
    public required bool Allowed { get; init; }

    /// <summary>Remaining requests allowed in the current window.</summary>
    public required int RemainingRequests { get; init; }

    /// <summary>Time to wait before retrying when throttled.</summary>
    public required TimeSpan RetryAfter { get; init; }
}
