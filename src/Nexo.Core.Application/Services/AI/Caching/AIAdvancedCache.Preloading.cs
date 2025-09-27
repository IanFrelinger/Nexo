using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Caching
{
    /// <summary>
    /// Cache preloading and warmup functionality for AIAdvancedCache.
    /// </summary>
    public partial class AIAdvancedCache
    {
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
    }
}
