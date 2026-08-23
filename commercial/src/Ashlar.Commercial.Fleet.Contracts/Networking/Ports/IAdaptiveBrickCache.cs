using Ashlar.Commercial.Fleet.Contracts.Networking.Models;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Commercial.Fleet.Contracts.Networking.Ports;
/// <summary>
/// Usage-aware brick cache that wraps a brick registry with dynamic TTL:
/// hot bricks get longer cache, cold bricks get short TTL, unused get evicted.
/// </summary>
public interface IAdaptiveBrickCache : IBrickRegistry
{
    /// <summary>Get cache statistics (hit rate, entries, evictions).</summary>
    AdaptiveBrickCacheStats GetCacheStats();
}
