using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Common.Ports;
using System.Collections.Concurrent;

namespace Nexo.Infrastructure.Caching;

/// <summary>
/// In-memory cache implementation using ConcurrentDictionary.
/// 
/// Responsibilities:
/// - Stores cached values in memory with expiration support
/// - Thread-safe implementation using concurrent collections
/// - Automatic expiration of expired entries
/// - Cache management (get, set, remove, clear)
/// 
/// Implements ICacheStrategy for use with cached service adapters.
/// Used by CachedAnalysisServiceAdapter and CachedValidationServiceAdapter.
/// </summary>
public class MemoryCacheStrategy : ICacheStrategy
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly ILogger<MemoryCacheStrategy> _logger;

    /// <summary>Initializes a new memory cache strategy.</summary>
    public MemoryCacheStrategy(ILogger<MemoryCacheStrategy> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Gets a cached value by key, removing expired entries.</summary>
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

    /// <summary>Stores a value in the cache with optional expiration.</summary>
    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var expiresAt = DateTime.UtcNow.Add(expiration ?? TimeSpan.FromMinutes(30));
        _cache.AddOrUpdate(key, new CacheEntry(value, expiresAt), (k, old) => new CacheEntry(value, expiresAt));
        _logger.LogDebug("Cache set for key: {Key}, expires at: {ExpiresAt}", key, expiresAt);
        return Task.CompletedTask;
    }

    /// <summary>Remove asynchronously.</summary>
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.TryRemove(key, out _);
        _logger.LogDebug("Cache removed for key: {Key}", key);
        return Task.CompletedTask;
    }

    /// <summary>Clear asynchronously.</summary>
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _cache.Clear();
        _logger.LogInformation("Cache cleared");
        return Task.CompletedTask;
    }

    private record CacheEntry(object Value, DateTime ExpiresAt);
}

