using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Ashlar.Orchestration.RateLimiting;

/// <summary>
/// Exception thrown when rate limit is exceeded.
/// </summary>
public sealed class RateLimitExceededException : Exception
{
    public RateLimitExceededException(string message) : base(message) { }
}
