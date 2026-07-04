using Nexo.Core.Domain.Bricks;

namespace Nexo.Core.Domain.Execution.Events;

// Cache events
/// <summary>
/// Emitted when a step result is served from cache.
/// </summary>
public class CacheHitEvent : ExecutionEvent
{
    /// <summary>ID of the step.</summary>
    public string StepId { get; init; } = default!;
    /// <summary>Cache key used.</summary>
    public string CacheKey { get; init; } = default!;
    
    public CacheHitEvent(string stepId, string cacheKey)
        : base("cache_hit", DateTime.UtcNow)
    {
        StepId = stepId;
        CacheKey = cacheKey;
    }
}
