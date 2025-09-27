using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Caching
{
    /// <summary>
    /// Cache health monitoring for AIAdvancedCache.
    /// </summary>
    public partial class AIAdvancedCache
    {
        /// <summary>
        /// Gets cache statistics
        /// </summary>
        public Task<CacheStatistics> GetStatisticsAsync()
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

                return Task.FromResult(_statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get cache statistics");
                throw;
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

        /// <summary>
        /// Calculates memory usage
        /// </summary>
        private double CalculateMemoryUsage()
        {
            // Simulate memory usage calculation
            return _cache.Count * 0.001; // 1KB per entry
        }

        /// <summary>
        /// Calculates eviction rate
        /// </summary>
        private double CalculateEvictionRate()
        {
            var totalOperations = _statistics.Hits + _statistics.Misses + _statistics.Sets;
            return totalOperations > 0 ? (double)_statistics.Evictions / totalOperations * 100 : 0;
        }
    }
}
