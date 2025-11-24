using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Common.Ports;
using System.Collections.Concurrent;

namespace Nexo.Infrastructure.Caching;

/// <summary>
/// In-memory cache implementation using ConcurrentDictionary.
/// </summary>
public class MemoryCacheStrategy : ICacheStrategy
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly ILogger<MemoryCacheStrategy> _logger;

    public MemoryCacheStrategy(ILogger<MemoryCacheStrategy> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.ExpiresAt > DateTime.UtcNow)
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return Task.FromResult<T?>(entry.Value as T);
            }
            else
            {
                // Expired, remove it
                _cache.TryRemove(key, out _);
                _logger.LogDebug("Cache entry expired for key: {Key}", key);
            }
        }
        else
        {
            _logger.LogDebug("Cache miss for key: {Key}", key);
        }

        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var expiresAt = DateTime.UtcNow.Add(expiration ?? TimeSpan.FromMinutes(30));
        _cache.AddOrUpdate(key, new CacheEntry(value, expiresAt), (k, old) => new CacheEntry(value, expiresAt));
        _logger.LogDebug("Cache set for key: {Key}, expires at: {ExpiresAt}", key, expiresAt);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.TryRemove(key, out _);
        _logger.LogDebug("Cache removed for key: {Key}", key);
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _cache.Clear();
        _logger.LogInformation("Cache cleared");
        return Task.CompletedTask;
    }

    private record CacheEntry(object Value, DateTime ExpiresAt);
}

