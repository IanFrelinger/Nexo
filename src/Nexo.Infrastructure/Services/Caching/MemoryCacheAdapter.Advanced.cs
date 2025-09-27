using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Caching;
using System.Collections.Concurrent;

namespace Nexo.Infrastructure.Services.Caching
{
    /// <summary>
    /// Advanced cache functionality
    /// </summary>
    public partial class MemoryCacheAdapter
    {
        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, DistributedCacheEntryOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            ArgumentNullException.ThrowIfNull(factory);

            var cachedValue = await GetAsync<T>(key, cancellationToken);
            if (cachedValue != null)
                return cachedValue;

            var factoryLock = _perKeyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            if (!await factoryLock.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken))
            {
                _logger.LogError($"GetOrSetAsync TIMEOUT acquiring factoryLock for key: {key}");
                throw new TimeoutException($"Timeout acquiring factoryLock in GetOrSetAsync for key: {key}");
            }
            try
            {
                // Double-check after acquiring the lock
                cachedValue = await GetAsync<T>(key, cancellationToken);
                if (cachedValue != null)
                    return cachedValue;

                // Execute factory and cache result
                var newValue = await factory();
                await SetAsync(key, newValue, options, cancellationToken);
                return newValue;
            }
            finally
            {
                factoryLock.Release();
                // Optionally clean up unused locks (not strictly necessary, but helps with memory)
                if (factoryLock.CurrentCount == 1)
                    _perKeyLocks.TryRemove(key, out _);
            }
        }
    }
}
