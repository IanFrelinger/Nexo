using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Ashlar.Orchestration.RateLimiting;

/// <summary>
/// Current rate limit status.
/// </summary>
public sealed record RateLimitStatus
{
    /// <summary>Rate limit bucket key.</summary>
    public required string Key { get; init; }

    /// <summary>Remaining requests allowed in the current window.</summary>
    public required int RemainingRequests { get; init; }

    /// <summary>Time until the rate limit window resets.</summary>
    public required TimeSpan ResetAfter { get; init; }

    /// <summary>Maximum requests allowed per window.</summary>
    public required int Limit { get; init; }
}
