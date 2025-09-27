using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Caching;

namespace Nexo.Infrastructure.Services.Caching
{
    /// <summary>
    /// Core cache operations functionality
    /// </summary>
    public partial class MemoryCacheAdapter
    {
        public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default(CancellationToken))
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

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (typeof(T) == typeof(string))
            {
                var value = await GetAsync(key, cancellationToken);
                return value == null ? default(T?) : (T)(object)value;
            }
            var valueStr = await GetAsync(key, cancellationToken);
            if (valueStr == null)
                return default(T?);
            try
            {
                return _serializer.Deserialize<T>(valueStr);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing cached value for key: {Key}", key);
                await RemoveAsync(key, cancellationToken);
                return default(T?);
            }
        }

        public async Task SetAsync(string key, string value, DistributedCacheEntryOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
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

        public async Task SetAsync<T>(string key, T value, DistributedCacheEntryOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
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
    }
}
