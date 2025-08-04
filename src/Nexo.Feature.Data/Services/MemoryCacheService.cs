using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Data.Interfaces;

namespace Nexo.Feature.Data.Services
{
    /// <summary>
    /// In-memory cache service implementation
    /// </summary>
    public class MemoryCacheService : ICacheService
    {
        private readonly ILogger<MemoryCacheService> _logger;
        private readonly ConcurrentDictionary<string, CacheItem> _cache;
        private readonly object _lockObject = new object();
        private long _hitCount;
        private long _missCount;
        private long _evictedKeys;
        private long _expiredKeys;

        public MemoryCacheService(ILogger<MemoryCacheService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = new ConcurrentDictionary<string, CacheItem>();
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                return default;

            try
            {
                if (_cache.TryGetValue(key, out var item))
                {
                    if (item.IsExpired)
                    {
                        _cache.TryRemove(key, out _);
                        Interlocked.Increment(ref _expiredKeys);
                        Interlocked.Increment(ref _missCount);
                        return default;
                    }

                    Interlocked.Increment(ref _hitCount);
                    return (T?)item.Value;
                }

                Interlocked.Increment(ref _missCount);
                return default;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting value from cache for key: {Key}", key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                return;

            try
            {
                var item = new CacheItem
                {
                    Value = value,
                    ExpirationTime = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : null
                };

                _cache.AddOrUpdate(key, item, (_, _) => item);
                
                // Clean up expired items periodically
                if (_cache.Count % 100 == 0)
                {
                    CleanupExpiredItems();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting value in cache for key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                return;

            try
            {
                _cache.TryRemove(key, out _);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing value from cache for key: {Key}", key);
            }
        }

        public async Task RemovePatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(pattern))
                return;

            try
            {
                var keysToRemove = _cache.Keys
                    .Where(key => key.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _cache.TryRemove(key, out _);
                }

                _logger.LogInformation("Removed {Count} cache entries matching pattern: {Pattern}", keysToRemove.Count, pattern);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache entries for pattern: {Pattern}", pattern);
            }
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            try
            {
                if (_cache.TryGetValue(key, out var item))
                {
                    if (item.IsExpired)
                    {
                        _cache.TryRemove(key, out _);
                        Interlocked.Increment(ref _expiredKeys);
                        return false;
                    }
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence in cache for key: {Key}", key);
                return false;
            }
        }

        public async Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            lock (_lockObject)
            {
                return new CacheStatistics
                {
                    TotalKeys = _cache.Count,
                    HitCount = _hitCount,
                    MissCount = _missCount,
                    EvictedKeys = _evictedKeys,
                    ExpiredKeys = _expiredKeys,
                    LastReset = DateTime.UtcNow
                };
            }
        }

        private void CleanupExpiredItems()
        {
            try
            {
                var expiredKeys = _cache
                    .Where(kvp => kvp.Value.IsExpired)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    if (_cache.TryRemove(key, out _))
                    {
                        Interlocked.Increment(ref _expiredKeys);
                    }
                }

                if (expiredKeys.Count > 0)
                {
                    _logger.LogDebug("Cleaned up {Count} expired cache entries", expiredKeys.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache cleanup");
            }
        }

        private class CacheItem
        {
            public object? Value { get; set; }
            public DateTime? ExpirationTime { get; set; }
            public bool IsExpired => ExpirationTime.HasValue && DateTime.UtcNow > ExpirationTime.Value;
        }
    }
} 