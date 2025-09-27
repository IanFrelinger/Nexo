using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Caching;

namespace Nexo.Infrastructure.Services.Caching
{
    /// <summary>
    /// Cache eviction and capacity management functionality
    /// </summary>
    public partial class MemoryCacheAdapter
    {
        private async Task EnsureCapacityAsync(long newItemSize, CancellationToken cancellationToken)
        {
            var itemsToEvict = new List<string>();
            await _cacheLock.WaitAsync(cancellationToken);
            try
            {
                // Check if we need to evict items due to size limit
                if (_currentSizeBytes + newItemSize > _maxSizeBytes)
                {
                    var items = _cache.Values.ToList();
                    var sizeToFree = _currentSizeBytes + newItemSize - _maxSizeBytes;
                    var itemsToRemove = _evictionPolicy.SelectForEviction(items, (int)(sizeToFree / 1024));
                    itemsToEvict.AddRange(itemsToRemove.Select(i => i.Key));
                }
                // Check if we need to evict items due to count limit
                if (_cache.Count >= _maxItems)
                {
                    var items = _cache.Values.ToList();
                    var itemsToRemove = _evictionPolicy.SelectForEviction(items, _cache.Count - _maxItems + 1);
                    itemsToEvict.AddRange(itemsToRemove.Select(i => i.Key));
                }
            }
            finally
            {
                _cacheLock.Release();
            }
            // Remove evicted items outside the lock
            foreach (var key in itemsToEvict.Distinct())
            {
                await RemoveAsync(key, cancellationToken);
                _statistics.Evictions++;
            }
        }

        private static bool IsExpired(CacheItem item)
        {
            return item.ExpiresAt <= DateTime.UtcNow;
        }
    }
}
