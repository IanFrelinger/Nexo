using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces;
using System;

namespace Nexo.Core.Application.Services
{
    /// <summary>
    /// In-memory thread-safe cache strategy implementation.
    /// </summary>
    /// <typeparam name="TKey">The type of the cache key.</typeparam>
    /// <typeparam name="TValue">The type of the cached value.</typeparam>
    public class CacheStrategy<TKey, TValue> : ICacheStrategy<TKey, TValue>
    {
        private class CacheEntry
        {
            public TValue Value { get; set; }
            public DateTimeOffset? Expiration { get; set; }
        }
        private readonly ConcurrentDictionary<TKey, CacheEntry> _cache = new ConcurrentDictionary<TKey, CacheEntry>();
        private readonly TimeSpan? _defaultTtl;

        public CacheStrategy(TimeSpan? defaultTtl = null)
        {
            _defaultTtl = defaultTtl;
        }

        /// <inheritdoc />
        public Task<TValue> GetAsync(TKey key, CancellationToken cancellationToken = default)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (entry.Expiration.HasValue && entry.Expiration.Value < DateTimeOffset.UtcNow)
                {
                    // Expired, remove
                    _cache.TryRemove(key, out _);
                    return Task.FromResult(default(TValue));
                }
                return Task.FromResult(entry.Value);
            }
            return Task.FromResult(default(TValue));
        }

        /// <inheritdoc />
        public Task SetAsync(TKey key, TValue value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
        {
            var expiration = ttl.HasValue ? DateTimeOffset.UtcNow.Add(ttl.Value) : (_defaultTtl.HasValue ? DateTimeOffset.UtcNow.Add(_defaultTtl.Value) : (DateTimeOffset?)null);
            _cache[key] = new CacheEntry { Value = value, Expiration = expiration };
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task RemoveAsync(TKey key, CancellationToken cancellationToken = default)
        {
            _cache.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            _cache.Clear();
            return Task.CompletedTask;
        }

        public Task InvalidateAsync(TKey key, CancellationToken cancellationToken = default)
        {
            return RemoveAsync(key, cancellationToken);
        }

        /// <summary>
        /// Attempts to get a value from the cache by key.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The cached value, if found.</param>
        /// <returns>True if the value was found in the cache; otherwise, false.</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (entry.Expiration.HasValue && entry.Expiration.Value < DateTimeOffset.UtcNow)
                {
                    _cache.TryRemove(key, out _);
                    value = default(TValue);
                    return false;
                }
                value = entry.Value;
                return true;
            }
            value = default(TValue);
            return false;
        }
    }
} 