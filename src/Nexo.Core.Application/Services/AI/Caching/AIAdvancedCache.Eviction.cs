using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Caching
{
    /// <summary>
    /// Cache eviction and refresh functionality for AIAdvancedCache.
    /// </summary>
    public partial class AIAdvancedCache
    {
        /// <summary>
        /// Determines if cache entry should be refreshed
        /// </summary>
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

        /// <summary>
        /// Refreshes cache entry
        /// </summary>
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

        /// <summary>
        /// Applies eviction policy
        /// </summary>
        private Task ApplyEvictionPolicyAsync()
        {
            try
            {
                var maxSize = GetPolicy("default").MaxSize;
                
                if (_cache.Count <= maxSize)
                    return Task.CompletedTask;

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
            
            return Task.CompletedTask;
        }
    }
}
