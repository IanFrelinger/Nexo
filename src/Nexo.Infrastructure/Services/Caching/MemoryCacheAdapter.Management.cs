using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Caching;

namespace Nexo.Infrastructure.Services.Caching
{
    /// <summary>
    /// Cache management functionality
    /// </summary>
    public partial class MemoryCacheAdapter
    {
        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("RemoveAsync START for key: {Key}", key);
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            if (!await _cacheLock.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken))
            {
                _logger.LogError($"RemoveAsync TIMEOUT acquiring lock for key: {key}");
                throw new TimeoutException($"Timeout acquiring _cacheLock in RemoveAsync for key: {key}");
            }
            _logger.LogDebug($"RemoveAsync ACQUIRED LOCK for key: {key}");
            try
            {
                if (_cache.TryRemove(key, out var item))
                {
                    _currentSizeBytes -= item.SizeBytes;
                    _logger.LogDebug("Removed cache item for key: {Key}", key);
                }
            }
            finally
            {
                _cacheLock.Release();
                _logger.LogDebug($"RemoveAsync RELEASED LOCK for key: {key}");
            }
            _logger.LogDebug($"RemoveAsync END for key: {key}");
        }

        public async Task RefreshAsync(string key, CancellationToken cancellationToken = default(CancellationToken))
        {
            _logger.LogDebug($"RefreshAsync START for key: {key}");
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            if (!await _cacheLock.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken))
            {
                _logger.LogError($"RefreshAsync TIMEOUT acquiring lock for key: {key}");
                throw new TimeoutException($"Timeout acquiring _cacheLock in RefreshAsync for key: {key}");
            }
            _logger.LogDebug($"RefreshAsync ACQUIRED LOCK for key: {key}");
            try
            {
                if (_cache.TryGetValue(key, out var item))
                {
                    item.LastAccessedAt = DateTime.UtcNow;
                    _logger.LogDebug("Refreshed cache item for key: {Key}", key);
                }
            }
            finally
            {
                _cacheLock.Release();
                _logger.LogDebug($"RefreshAsync RELEASED LOCK for key: {key}");
            }
            _logger.LogDebug($"RefreshAsync END for key: {key}");
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            if (!_cache.TryGetValue(key, out var item)) return false;
            if (!IsExpired(item)) return true;
            await RemoveAsync(key, cancellationToken);
            return false;
        }

        public async Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            _logger.LogDebug($"GetStatisticsAsync START");
            if (!await _cacheLock.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken))
            {
                _logger.LogError($"GetStatisticsAsync TIMEOUT acquiring lock");
                throw new TimeoutException($"Timeout acquiring _cacheLock in GetStatisticsAsync");
            }
            _logger.LogDebug($"GetStatisticsAsync ACQUIRED LOCK");
            try
            {
                _statistics.TotalItems = _cache.Count;
                _statistics.MemoryUsageBytes = _currentSizeBytes;
                return _statistics;
            }
            finally
            {
                _cacheLock.Release();
                _logger.LogDebug($"GetStatisticsAsync RELEASED LOCK");
            }
        }

        public async Task ClearAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            _logger.LogDebug($"ClearAsync START");
            if (!await _cacheLock.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken))
            {
                _logger.LogError($"ClearAsync TIMEOUT acquiring lock");
                throw new TimeoutException($"Timeout acquiring _cacheLock in ClearAsync");
            }
            _logger.LogDebug($"ClearAsync ACQUIRED LOCK");
            try
            {
                _cache.Clear();
                _currentSizeBytes = 0;
                _statistics.TotalItems = 0;
                _statistics.MemoryUsageBytes = 0;
                _logger.LogInformation("Cache cleared");
            }
            finally
            {
                _cacheLock.Release();
                _logger.LogDebug($"ClearAsync RELEASED LOCK");
            }
            _logger.LogDebug($"ClearAsync END");
        }
    }
}
