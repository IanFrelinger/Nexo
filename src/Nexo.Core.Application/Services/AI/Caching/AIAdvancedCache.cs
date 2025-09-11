using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Caching
{
    /// <summary>
    /// Advanced AI caching service with intelligent caching strategies
    /// </summary>
    public class AIAdvancedCache
    {
        private readonly ILogger<AIAdvancedCache> _logger;
        private readonly Dictionary<string, CacheEntry> _cache;
        private readonly Dictionary<string, CachePolicy> _policies;
        private readonly object _lockObject = new object();
        private readonly CacheStatistics _statistics;

        public AIAdvancedCache(ILogger<AIAdvancedCache> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = new Dictionary<string, CacheEntry>();
            _policies = new Dictionary<string, CachePolicy>();
            _statistics = new CacheStatistics();
        }

        /// <summary>
        /// Gets a cached value
        /// </summary>
        public async Task<CacheResult<T>> GetAsync<T>(string key, string? policyName = null)
        {
            try
            {
                _logger.LogDebug("Getting cached value for key {Key}", key);

                lock (_lockObject)
                {
                    if (_cache.TryGetValue(key, out var entry))
                    {
                        // Check if entry is expired
                        if (entry.ExpiresAt.HasValue && DateTime.UtcNow > entry.ExpiresAt.Value)
                        {
                            _cache.Remove(key);
                            _statistics.ExpiredHits++;
                            _logger.LogDebug("Cache entry {Key} expired and removed", key);
                            return new CacheResult<T> { Found = false };
                        }

                        // Update access statistics
                        entry.AccessCount++;
                        entry.LastAccessedAt = DateTime.UtcNow;
                        _statistics.Hits++;

                        // Check if we need to refresh
                        if (ShouldRefresh(entry, policyName))
                        {
                            _ = Task.Run(() => RefreshEntryAsync(key, entry));
                        }

                        _logger.LogDebug("Cache hit for key {Key}", key);
                        return new CacheResult<T>
                        {
                            Found = true,
                            Value = (T)entry.Value,
                            Metadata = entry.Metadata
                        };
                    }
                }

                _statistics.Misses++;
                _logger.LogDebug("Cache miss for key {Key}", key);
                return new CacheResult<T> { Found = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get cached value for key {Key}", key);
                return new CacheResult<T> { Found = false };
            }
        }

        /// <summary>
        /// Sets a cached value
        /// </summary>
        public async Task<bool> SetAsync<T>(string key, T value, string? policyName = null, Dictionary<string, object>? metadata = null)
        {
            try
            {
                _logger.LogDebug("Setting cached value for key {Key}", key);

                var policy = GetPolicy(policyName);
                var expiresAt = policy.ExpirationTime.HasValue ? (DateTime?)DateTime.UtcNow.Add(policy.ExpirationTime.Value) : null;

                var entry = new CacheEntry
                {
                    Key = key,
                    Value = value!,
                    CreatedAt = DateTime.UtcNow,
                    LastAccessedAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt,
                    AccessCount = 0,
                    PolicyName = policyName ?? "default",
                    Metadata = metadata ?? new Dictionary<string, object>()
                };

                lock (_lockObject)
                {
                    _cache[key] = entry;
                    _statistics.Sets++;
                }

                // Apply eviction if needed
                await ApplyEvictionPolicyAsync();

                _logger.LogDebug("Cached value set for key {Key}", key);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set cached value for key {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// Removes a cached value
        /// </summary>
        public async Task<bool> RemoveAsync(string key)
        {
            try
            {
                _logger.LogDebug("Removing cached value for key {Key}", key);

                lock (_lockObject)
                {
                    if (_cache.Remove(key))
                    {
                        _statistics.Removals++;
                        _logger.LogDebug("Cached value removed for key {Key}", key);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove cached value for key {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// Clears all cached values
        /// </summary>
        public async Task ClearAsync()
        {
            try
            {
                _logger.LogInformation("Clearing all cached values");

                lock (_lockObject)
                {
                    _cache.Clear();
                    _statistics.Clears++;
                }

                _logger.LogInformation("All cached values cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear cached values");
            }
        }

        /// <summary>
        /// Gets cache statistics
        /// </summary>
        public async Task<CacheStatistics> GetStatisticsAsync()
        {
            try
            {
                lock (_lockObject)
                {
                    _statistics.TotalEntries = _cache.Count;
                    _statistics.HitRate = _statistics.Hits + _statistics.Misses > 0 
                        ? (double)_statistics.Hits / (_statistics.Hits + _statistics.Misses) * 100 
                        : 0;
                    _statistics.LastUpdated = DateTime.UtcNow;
                }

                return _statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get cache statistics");
                throw;
            }
        }

        /// <summary>
        /// Creates a cache policy
        /// </summary>
        public async Task<bool> CreatePolicyAsync(string policyName, CachePolicy policy)
        {
            try
            {
                _logger.LogInformation("Creating cache policy {PolicyName}", policyName);

                lock (_lockObject)
                {
                    _policies[policyName] = policy;
                }

                _logger.LogInformation("Cache policy {PolicyName} created successfully", policyName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create cache policy {PolicyName}", policyName);
                return false;
            }
        }

        /// <summary>
        /// Gets cache policy
        /// </summary>
        public async Task<CachePolicy?> GetPolicyAsync(string policyName)
        {
            try
            {
                lock (_lockObject)
                {
                    _policies.TryGetValue(policyName, out var policy);
                    return policy;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get cache policy {PolicyName}", policyName);
                return null;
            }
        }

        /// <summary>
        /// Preloads cache with frequently accessed data
        /// </summary>
        public async Task PreloadCacheAsync(List<PreloadItem> items)
        {
            try
            {
                _logger.LogInformation("Preloading cache with {ItemCount} items", items.Count);

                foreach (var item in items)
                {
                    await SetAsync(item.Key, item.Value, item.PolicyName, item.Metadata);
                }

                _logger.LogInformation("Cache preloading completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to preload cache");
            }
        }

        /// <summary>
        /// Warms up cache with predictive loading
        /// </summary>
        public async Task WarmupCacheAsync(List<string> keys, Func<string, Task<object>> valueFactory)
        {
            try
            {
                _logger.LogInformation("Warming up cache with {KeyCount} keys", keys.Count);

                var warmupTasks = keys.Select(async key =>
                {
                    try
                    {
                        var value = await valueFactory(key);
                        await SetAsync(key, value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to warm up cache for key {Key}", key);
                    }
                });

                await Task.WhenAll(warmupTasks);

                _logger.LogInformation("Cache warmup completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to warm up cache");
            }
        }

        /// <summary>
        /// Gets cache health information
        /// </summary>
        public async Task<CacheHealth> GetHealthAsync()
        {
            try
            {
                var statistics = await GetStatisticsAsync();
                
                var health = new CacheHealth
                {
                    IsHealthy = true,
                    HitRate = statistics.HitRate,
                    TotalEntries = statistics.TotalEntries,
                    MemoryUsage = CalculateMemoryUsage(),
                    EvictionRate = CalculateEvictionRate(),
                    LastUpdated = DateTime.UtcNow
                };

                // Determine health status
                if (statistics.HitRate < 50)
                {
                    health.IsHealthy = false;
                    health.Issues.Add("Low hit rate detected");
                }

                if (statistics.TotalEntries > 10000)
                {
                    health.IsHealthy = false;
                    health.Issues.Add("High memory usage detected");
                }

                return health;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get cache health");
                return new CacheHealth
                {
                    IsHealthy = false,
                    Issues = new List<string> { $"Health check failed: {ex.Message}" }
                };
            }
        }

        private CachePolicy GetPolicy(string? policyName)
        {
            if (string.IsNullOrEmpty(policyName))
                policyName = "default";

            lock (_lockObject)
            {
                if (_policies.TryGetValue(policyName, out var policy))
                    return policy;
            }

            // Return default policy
            return new CachePolicy
            {
                Name = "default",
                ExpirationTime = TimeSpan.FromHours(1),
                MaxSize = 1000,
                EvictionStrategy = EvictionStrategy.LRU
            };
        }

        private bool ShouldRefresh(CacheEntry entry, string? policyName)
        {
            var policy = GetPolicy(policyName);
            
            // Refresh if entry is close to expiration
            if (entry.ExpiresAt.HasValue && DateTime.UtcNow.AddMinutes(5) > entry.ExpiresAt.Value)
                return true;

            // Refresh if access count is high (frequently accessed)
            if (entry.AccessCount > 100)
                return true;

            return false;
        }

        private async Task RefreshEntryAsync(string key, CacheEntry entry)
        {
            try
            {
                _logger.LogDebug("Refreshing cache entry {Key}", key);

                // Simulate refresh operation
                await Task.Delay(100);

                // Update entry
                lock (_lockObject)
                {
                    if (_cache.TryGetValue(key, out var currentEntry))
                    {
                        currentEntry.LastRefreshedAt = DateTime.UtcNow;
                        currentEntry.AccessCount = 0; // Reset access count after refresh
                    }
                }

                _logger.LogDebug("Cache entry {Key} refreshed successfully", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh cache entry {Key}", key);
            }
        }

        private async Task ApplyEvictionPolicyAsync()
        {
            try
            {
                var maxSize = GetPolicy("default").MaxSize;
                
                if (_cache.Count <= maxSize)
                    return;

                _logger.LogDebug("Applying eviction policy, current size: {CurrentSize}, max size: {MaxSize}", 
                    _cache.Count, maxSize);

                var entriesToRemove = _cache.Values
                    .OrderBy(e => e.LastAccessedAt)
                    .Take(_cache.Count - maxSize)
                    .ToList();

                foreach (var entry in entriesToRemove)
                {
                    _cache.Remove(entry.Key);
                    _statistics.Evictions++;
                }

                _logger.LogDebug("Evicted {EvictionCount} entries", entriesToRemove.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply eviction policy");
            }
        }

        private double CalculateMemoryUsage()
        {
            // Simulate memory usage calculation
            return _cache.Count * 0.001; // 1KB per entry
        }

        private double CalculateEvictionRate()
        {
            var totalOperations = _statistics.Hits + _statistics.Misses + _statistics.Sets;
            return totalOperations > 0 ? (double)_statistics.Evictions / totalOperations * 100 : 0;
        }
    }

    /// <summary>
    /// Cache entry
    /// </summary>
    public class CacheEntry
    {
        public string Key { get; set; } = string.Empty;
        public object Value { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime LastAccessedAt { get; set; }
        public DateTime? LastRefreshedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int AccessCount { get; set; }
        public string PolicyName { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Cache policy
    /// </summary>
    public class CachePolicy
    {
        public string Name { get; set; } = string.Empty;
        public TimeSpan? ExpirationTime { get; set; }
        public int MaxSize { get; set; } = 1000;
        public EvictionStrategy EvictionStrategy { get; set; } = EvictionStrategy.LRU;
        public bool EnableRefresh { get; set; } = true;
        public TimeSpan? RefreshThreshold { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Cache result
    /// </summary>
    public class CacheResult<T>
    {
        public bool Found { get; set; }
        public T? Value { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Cache statistics
    /// </summary>
    public class CacheStatistics
    {
        public int TotalEntries { get; set; }
        public int Hits { get; set; }
        public int Misses { get; set; }
        public int Sets { get; set; }
        public int Removals { get; set; }
        public int Evictions { get; set; }
        public int ExpiredHits { get; set; }
        public int Clears { get; set; }
        public double HitRate { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Cache health
    /// </summary>
    public class CacheHealth
    {
        public bool IsHealthy { get; set; }
        public double HitRate { get; set; }
        public int TotalEntries { get; set; }
        public double MemoryUsage { get; set; }
        public double EvictionRate { get; set; }
        public List<string> Issues { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Preload item
    /// </summary>
    public class PreloadItem
    {
        public string Key { get; set; } = string.Empty;
        public object Value { get; set; } = new();
        public string? PolicyName { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Eviction strategies
    /// </summary>
    public enum EvictionStrategy
    {
        LRU,    // Least Recently Used
        LFU,    // Least Frequently Used
        FIFO,   // First In, First Out
        TTL     // Time To Live
    }
}
