using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.AI.Interfaces
{
    /// <summary>
    /// Defines a strategy for caching and retrieving values by key.
    /// </summary>
    /// <typeparam name="TKey">The type of the cache key.</typeparam>
    /// <typeparam name="TValue">The type of the cached value.</typeparam>
    public interface ICacheStrategy<TKey, TValue>
    {
        /// <summary>
        /// Gets a value from the cache by key.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The cached value, or default if not found.</returns>
        Task<TValue> GetAsync(TKey key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets a value in the cache for the specified key.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache.</param>
        /// <param name="ttl">Time-to-live for the cached value (optional).</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task representing the set operation.</returns>
        Task SetAsync(TKey key, TValue value, TimeSpan? ttl = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a value from the cache by key.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task representing the remove operation.</returns>
        Task RemoveAsync(TKey key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes all entries from the cache.
        /// </summary>
        Task ClearAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates the cache entry for the given key (alias for RemoveAsync).
        /// </summary>
        Task InvalidateAsync(TKey key, CancellationToken cancellationToken = default);
    }
} 