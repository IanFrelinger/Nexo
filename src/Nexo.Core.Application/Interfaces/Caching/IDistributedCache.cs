using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces.Caching
{
    /// <summary>
    /// Interface for distributed caching operations.
    /// </summary>
    public interface IDistributedCache
    {
        /// <summary>
        /// Gets a value from the cache.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached value, or null if not found.</returns>
        Task<string?> GetAsync(string key, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets a value from the cache and deserializes it to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The deserialized value, or null if not found.</returns>
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Sets a value in the cache.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache.</param>
        /// <param name="options">Cache options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SetAsync(string key, string value, DistributedCacheEntryOptions? options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Sets a value in the cache by serializing the object.
        /// </summary>
        /// <typeparam name="T">The type of the value to serialize.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache.</param>
        /// <param name="options">Cache options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SetAsync<T>(string key, T value, DistributedCacheEntryOptions? options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Removes a value from the cache.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task RemoveAsync(string key, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Refreshes a value in the cache, extending its expiration time.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task RefreshAsync(string key, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets or sets a value atomically.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="factory">Factory function to create the value if not found.</param>
        /// <param name="options">Cache options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached or newly created value.</returns>
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, DistributedCacheEntryOptions? options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Checks if a key exists in the cache.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the key exists; otherwise, false.</returns>
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets cache statistics.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Cache statistics.</returns>
        Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Clears all items from the cache.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ClearAsync(CancellationToken cancellationToken = default(CancellationToken));
    }

    /// <summary>
    /// Options for distributed cache entries.
    /// </summary>
    public class DistributedCacheEntryOptions
    {
        /// <summary>
        /// Gets or sets the absolute expiration time.
        /// </summary>
        public DateTimeOffset AbsoluteExpiration { get; set; }

        /// <summary>
        /// Gets or sets the sliding expiration time.
        /// </summary>
        public TimeSpan SlidingExpiration { get; set; }

        /// <summary>
        /// Gets or sets the priority for eviction.
        /// </summary>
        public CacheItemPriority Priority { get; set; } = CacheItemPriority.Normal;

        /// <summary>
        /// Gets or sets custom metadata for the cache entry.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Priority levels for cache items.
    /// </summary>
    public enum CacheItemPriority
    {
        /// <summary>
        /// Low priority - first to be evicted.
        /// </summary>
        Low,

        /// <summary>
        /// Normal priority.
        /// </summary>
        Normal,

        /// <summary>
        /// High priority - last to be evicted.
        /// </summary>
        High,

        /// <summary>
        /// Never evict - persists until manually removed.
        /// </summary>
        NeverRemove
    }

    /// <summary>
    /// Statistics about the cache.
    /// </summary>
    public class CacheStatistics
    {
        /// <summary>
        /// Gets or sets the total number of items in the cache.
        /// </summary>
        public long TotalItems { get; set; }

        /// <summary>
        /// Gets or sets the total memory usage in bytes.
        /// </summary>
        public long MemoryUsageBytes { get; set; }

        /// <summary>
        /// Gets or sets the number of cache hits.
        /// </summary>
        public long Hits { get; set; }

        /// <summary>
        /// Gets or sets the number of cache misses.
        /// </summary>
        public long Misses { get; set; }

        /// <summary>
        /// Gets or sets the hit rate as a percentage.
        /// </summary>
        public double HitRate => TotalRequests > 0 ? (double)Hits / TotalRequests * 100 : 0;

        /// <summary>
        /// Gets or sets the total number of requests.
        /// </summary>
        public long TotalRequests => Hits + Misses;

        /// <summary>
        /// Gets or sets the number of evictions.
        /// </summary>
        public long Evictions { get; set; }

        /// <summary>
        /// Gets or sets the number of expirations.
        /// </summary>
        public long Expirations { get; set; }

        /// <summary>
        /// Gets or sets the last reset time.
        /// </summary>
        public DateTime LastResetTime { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Interface for cache serialization.
    /// </summary>
    public interface ICacheSerializer
    {
        /// <summary>
        /// Serializes an object to a string.
        /// </summary>
        /// <typeparam name="T">The type to serialize.</typeparam>
        /// <param name="value">The value to serialize.</param>
        /// <returns>The serialized string.</returns>
        string Serialize<T>(T value);

        /// <summary>
        /// Deserializes a string to an object.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="value">The string to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        T? Deserialize<T>(string value);
    }

    /// <summary>
    /// Interface for cache eviction policies.
    /// </summary>
    public interface ICacheEvictionPolicy
    {
        /// <summary>
        /// Determines which items should be evicted when the cache is full.
        /// </summary>
        /// <param name="items">The cache items to consider for eviction.</param>
        /// <param name="count">The number of items to evict.</param>
        /// <returns>The items to evict.</returns>
        IEnumerable<CacheItem> SelectForEviction(IEnumerable<CacheItem> items, int count);
    }

    /// <summary>
    /// Represents a cache item.
    /// </summary>
    public class CacheItem
    {
        /// <summary>
        /// Gets or sets the cache key.
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the cached value.
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the creation time.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the last accessed time.
        /// </summary>
        public DateTime LastAccessedAt { get; set; }

        /// <summary>
        /// Gets or sets the expiration time.
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the access count.
        /// </summary>
        public long AccessCount { get; set; }

        /// <summary>
        /// Gets or sets the priority.
        /// </summary>
        public CacheItemPriority Priority { get; set; }

        /// <summary>
        /// Gets or sets the size in bytes.
        /// </summary>
        public long SizeBytes { get; set; }

        /// <summary>
        /// Gets or sets the sliding expiration time.
        /// </summary>
        public TimeSpan SlidingExpiration { get; set; }
    }
} 