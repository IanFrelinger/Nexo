using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Data.Interfaces
{
    /// <summary>
    /// Cache service interface for repository caching strategies
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Gets a value from cache
        /// </summary>
        /// <typeparam name="T">Type of cached value</typeparam>
        /// <param name="key">Cache key</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Cached value if found, null otherwise</returns>
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets a value in cache
        /// </summary>
        /// <typeparam name="T">Type of value to cache</typeparam>
        /// <param name="key">Cache key</param>
        /// <param name="value">Value to cache</param>
        /// <param name="expiration">Cache expiration time</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a value from cache
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes all values with a specific pattern
        /// </summary>
        /// <param name="pattern">Cache key pattern</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task RemovePatternAsync(string pattern, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a key exists in cache
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if key exists, false otherwise</returns>
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets cache statistics
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Cache statistics</returns>
        Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Cache statistics
    /// </summary>
    public class CacheStatistics
    {
        public long TotalKeys { get; set; }
        public long HitCount { get; set; }
        public long MissCount { get; set; }
        public double HitRate => TotalRequests > 0 ? (double)HitCount / TotalRequests : 0;
        public long TotalRequests => HitCount + MissCount;
        public long EvictedKeys { get; set; }
        public long ExpiredKeys { get; set; }
        public DateTime LastReset { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Cache options for repository operations
    /// </summary>
    public class CacheOptions
    {
        public bool EnableCaching { get; set; } = true;
        public TimeSpan? Expiration { get; set; } = TimeSpan.FromMinutes(30);
        public string? KeyPrefix { get; set; }
        public bool InvalidateOnWrite { get; set; } = true;
        public bool InvalidatePattern { get; set; } = true;
    }
} 