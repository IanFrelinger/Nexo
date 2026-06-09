using Nexo.Core.Application.Networking.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Core.Application.Networking.Ports;

/// <summary>
/// Usage-aware brick cache that wraps a brick registry with dynamic TTL:
/// hot bricks get longer cache, cold bricks get short TTL, unused get evicted.
/// </summary>
public interface IAdaptiveBrickCache : IBrickRegistry
{
    /// <summary>Get cache statistics (hit rate, entries, evictions).</summary>
    AdaptiveBrickCacheStats GetCacheStats();
}
