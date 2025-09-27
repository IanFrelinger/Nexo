using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Caching
{
    /// <summary>
    /// Core cache operations for AIAdvancedCache.
    /// </summary>
    public partial class AIAdvancedCache
    {
        /// <summary>
        /// Gets a cached value
        /// </summary>
        public Task<CacheResult<T>> GetAsync<T>(string key, string? policyName = null)
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
                            return Task.FromResult(new CacheResult<T> { Found = false });
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
                        return Task.FromResult(new CacheResult<T>
                        {
                            Found = true,
                            Value = (T)entry.Value,
                            Metadata = entry.Metadata
                        });
                    }
                }

                _statistics.Misses++;
                _logger.LogDebug("Cache miss for key {Key}", key);
                return Task.FromResult(new CacheResult<T> { Found = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get cached value for key {Key}", key);
                return Task.FromResult(new CacheResult<T> { Found = false });
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
        public Task<bool> RemoveAsync(string key)
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
                        return Task.FromResult(true);
                    }
                }

                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove cached value for key {Key}", key);
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Clears all cached values
        /// </summary>
        public Task ClearAsync()
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
            
            return Task.CompletedTask;
        }
    }
}
