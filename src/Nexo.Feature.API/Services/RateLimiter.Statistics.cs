using Microsoft.Extensions.Logging;
using Nexo.Feature.API.Enums;
using Nexo.Feature.API.Models;
using System.Collections.Concurrent;

namespace Nexo.Feature.API.Services;

/// <summary>
/// Statistics and monitoring functionality for rate limiting
/// </summary>
public partial class RateLimiter
{
    /// <summary>
    /// Gets rate limiting statistics and metrics
    /// </summary>
    public async Task<RateLimitStatistics> GetStatisticsAsync()
    {
        lock (_statisticsLock)
        {
            var totalChecks = GetStatistic("TotalChecks");
            var totalRateLimited = GetStatistic("TotalRateLimited");

            return new RateLimitStatistics
            {
                TotalChecks = totalChecks,
                TotalRateLimited = totalRateLimited,
                ActiveConfigurations = _configurations.Count,
                StatisticsByScope = GetStatisticsByScope(),
                GeneratedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Increments a statistic counter
    /// </summary>
    private void IncrementStatistic(string key)
    {
        lock (_statisticsLock)
        {
            if (_statistics.ContainsKey(key))
            {
                _statistics[key]++;
            }
            else
            {
                _statistics[key] = 1;
            }
        }
    }

    /// <summary>
    /// Gets a statistic value
    /// </summary>
    private long GetStatistic(string key)
    {
        lock (_statisticsLock)
        {
            return _statistics.GetValueOrDefault(key, 0);
        }
    }

    /// <summary>
    /// Gets statistics grouped by scope
    /// </summary>
    private Dictionary<RateLimitScope, long> GetStatisticsByScope()
    {
        var result = new Dictionary<RateLimitScope, long>();
        foreach (RateLimitScope scope in Enum.GetValues(typeof(RateLimitScope)))
        {
            result[scope] = 0; // TODO: Implement scope-specific statistics
        }
        return result;
    }
}