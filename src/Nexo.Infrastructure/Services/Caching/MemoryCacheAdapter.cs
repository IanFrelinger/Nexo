using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Caching;
using System.Collections.Concurrent;
using System.Linq;

namespace Nexo.Infrastructure.Services.Caching
{
/// <summary>
/// In-memory cache adapter implementing IDistributedCache.
/// </summary>
public class MemoryCacheAdapter : IDistributedCache
{
    private readonly ILogger<MemoryCacheAdapter> _logger;
    private readonly ConcurrentDictionary<string, CacheItem> _cache = new();
    private readonly ICacheSerializer _serializer;
    private readonly ICacheEvictionPolicy _evictionPolicy;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private readonly Timer _cleanupTimer;
    private readonly long _maxSizeBytes;
    private readonly int _maxItems;

    private readonly CacheStatistics _statistics = new();
    private long _currentSizeBytes;

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _perKeyLocks = new();

    public MemoryCacheAdapter(
        ILogger<MemoryCacheAdapter> logger,
        ICacheSerializer serializer,
        ICacheEvictionPolicy evictionPolicy, Timer cleanupTimer, long maxSizeBytes = 100 * 1024 * 1024, // 100MB default
        int maxItems = 10000)
    {
            ArgumentNullException.ThrowIfNull(logger);
            serializer ??= new JsonCacheSerializer();
        evictionPolicy ??= new LruEvictionPolicy();
        _logger = logger;
        _serializer = serializer;
        _evictionPolicy = evictionPolicy;
        _cleanupTimer = cleanupTimer;
        _maxSizeBytes = maxSizeBytes;
        _maxItems = maxItems;

        // Start cleanup timer to remove expired items
        // _cleanupTimer = new Timer(CleanupExpiredItems, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

        _logger.LogInformation("Memory cache adapter initialized with max size: {MaxSize}MB, max items: {MaxItems}", 
            maxSizeBytes / (1024 * 1024), maxItems);
    }

    public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default(CancellationToken))
    {
        Console.WriteLine($"[Cache] GetAsync START for key: {key}");
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be null or empty.", nameof(key));

        try
        {
            if (_cache.TryGetValue(key, out var item))
            {
                if (IsExpired(item))
                {
                    await RemoveAsync(key, cancellationToken);
                    _statistics.Misses++;
                    Console.WriteLine($"[Cache] GetAsync END (expired) for key: {key}");
                    return null;
                }

                // Update access statistics
                item.LastAccessedAt = DateTime.UtcNow;
                item.AccessCount++;
                _statistics.Hits++;

                // Sliding expiration: update expiration on access
                item.ExpiresAt = DateTime.UtcNow.Add(item.SlidingExpiration);

                _logger.LogDebug("Cache hit for key: {Key}", key);
                Console.WriteLine($"[Cache] GetAsync END (hit) for key: {key}");
                return item.Value;
            }

            _statistics.Misses++;
            _logger.LogDebug("Cache miss for key: {Key}", key);
            Console.WriteLine($"[Cache] GetAsync END (miss) for key: {key}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting value from cache for key: {Key}", key);
            Console.WriteLine($"[Cache] GetAsync ERROR for key: {key}");
            throw;
        }
    }

    public async Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default(CancellationToken))
    {
        if (typeof(T) == typeof(string))
        {
            var value = await GetAsync(key, cancellationToken);
            return value == null ? default(T) : (T)(object)value;
        }
        var valueStr = await GetAsync(key, cancellationToken);
        if (valueStr == null)
            return default(T);
        try
        {
            return _serializer.Deserialize<T>(valueStr);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing cached value for key: {Key}", key);
            await RemoveAsync(key, cancellationToken);
            return default(T);
        }
    }

    public async Task SetAsync(string key, string value, DistributedCacheEntryOptions options = null!, CancellationToken cancellationToken = default(CancellationToken))
    {
        Console.WriteLine($"[Cache] SetAsync START for key: {key}");
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            ArgumentNullException.ThrowIfNull(value);

            var itemSize = System.Text.Encoding.UTF8.GetByteCount(value);
        if (itemSize > _maxSizeBytes)
            throw new InvalidOperationException($"Item size {itemSize} exceeds cache max size {_maxSizeBytes}");

        Console.WriteLine($"[Cache] SetAsync WAITING for _cacheLock for key: {key}");
        if (!await _cacheLock.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken))
        {
            _logger.LogError($"SetAsync TIMEOUT acquiring lock for key: {key}");
            Console.WriteLine($"[Cache] SetAsync TIMEOUT for _cacheLock for key: {key}");
            throw new TimeoutException($"Timeout acquiring _cacheLock in SetAsync for key: {key}");
        }
        Console.WriteLine($"[Cache] SetAsync ACQUIRED _cacheLock for key: {key}");
        try
        {
            var item = new CacheItem
            {
                Key = key,
                Value = value,
                CreatedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow,
                AccessCount = 0,
                Priority = options?.Priority ?? CacheItemPriority.Normal,
                SizeBytes = System.Text.Encoding.UTF8.GetByteCount(value)
            };

            // Set expiration
            if (options?.AbsoluteExpiration != null)
            {
                item.ExpiresAt = options.AbsoluteExpiration.UtcDateTime;
            }

            // Check if we need to evict items
            await EnsureCapacityAsync(item.SizeBytes, cancellationToken);

            // Add or update the item
            _cache.AddOrUpdate(key, item, (k, v) => item);
            _currentSizeBytes += item.SizeBytes;

            _logger.LogDebug("Cached value for key: {Key}, size: {Size} bytes", key, item.SizeBytes);
        }
        finally
        {
            _cacheLock.Release();
            Console.WriteLine($"[Cache] SetAsync RELEASED _cacheLock for key: {key}");
        }
        Console.WriteLine($"[Cache] SetAsync END for key: {key}");
    }

    public async Task SetAsync<T>(string key, T value, DistributedCacheEntryOptions options = null!, CancellationToken cancellationToken = default(CancellationToken))
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be null or empty.", nameof(key));
        if (value == null) throw new ArgumentNullException(nameof(value));

        if (value is string str)
        {
            if (options != null) await SetAsync(key, str, options, cancellationToken);
            return;
        }
        var serializedValue = _serializer.Serialize(value);
        var itemSize = System.Text.Encoding.UTF8.GetByteCount(serializedValue);
        if (itemSize > _maxSizeBytes)
            throw new InvalidOperationException($"Item size {itemSize} exceeds cache max size {_maxSizeBytes}");
        if (options != null) await SetAsync(key, serializedValue, options, cancellationToken);
    }

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

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, DistributedCacheEntryOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
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
        _logger.LogDebug($"GetStatisticsAsync END");
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

    // Comment out the CleanupExpiredItems method
    // private void CleanupExpiredItems(object? state)
    // {
    //     try
    //     {
    //         var expiredKeys = _cache.Values
    //             .Where(IsExpired)
    //             .Select(x => x.Key)
    //             .ToList();
    //
    //         foreach (var key in expiredKeys)
    //         {
    //             _cache.TryRemove(key, out _);
    //             _statistics.Expirations++;
    //         }
    //
    //         if (expiredKeys.Count > 0)
    //         {
    //             _logger.LogDebug("Cleaned up {Count} expired cache items", expiredKeys.Count);
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error during cache cleanup");
    //     }
    // }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        _cacheLock?.Dispose();
    }
}

/// <summary>
/// JSON-based cache serializer.
/// </summary>
public class JsonCacheSerializer : ICacheSerializer
{
    private readonly JsonSerializerOptions _options = new JsonSerializerOptions
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string Serialize<T>(T value)
    {
        if (value is string str)
        {
            Console.WriteLine($"[JsonCacheSerializer] Serialize<T>: T=string, value='{str}' (return as-is)");
            return str;
        }
        var serialized = JsonSerializer.Serialize(value, _options);
        Console.WriteLine($"[JsonCacheSerializer] Serialize<T>: T={typeof(T)}, value='{value}' => '{serialized}'");
        return serialized;
    }

    public T Deserialize<T>(string value)
    {
        if (typeof(T) == typeof(string))
        {
            Console.WriteLine($"[JsonCacheSerializer] Deserialize<T>: T=string, value='{value}' (return as-is)");
            return (T)(object)value;
        }
        var deserialized = JsonSerializer.Deserialize<T>(value, _options);
        Console.WriteLine($"[JsonCacheSerializer] Deserialize<T>: T={typeof(T)}, value='{value}' => '{deserialized}'");
        return deserialized;
    }
}

/// <summary>
/// LRU (Least Recently Used) eviction policy.
/// </summary>
public class LruEvictionPolicy : ICacheEvictionPolicy
{
    public IEnumerable<CacheItem> SelectForEviction(IEnumerable<CacheItem> items, int count)
    {
        return items
            .OrderBy(x => x.LastAccessedAt)
            .ThenBy(x => x.Priority)
            .Take(count);
    }
}
}