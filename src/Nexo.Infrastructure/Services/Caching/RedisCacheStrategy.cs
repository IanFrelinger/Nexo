using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces;
using StackExchange.Redis;

namespace Nexo.Infrastructure.Services.Caching
{
    /// <summary>
    /// Redis implementation of ICacheStrategy for distributed caching.
    /// </summary>
    public class RedisCacheStrategy<TKey, TValue> : ICacheStrategy<TKey, TValue>
    {
        private readonly IDatabase _database;
        private readonly string _keyPrefix;
        private readonly JsonSerializerOptions _jsonOptions;

        public RedisCacheStrategy(IConnectionMultiplexer redis, string keyPrefix = "nexo:cache:", JsonSerializerOptions? jsonOptions = null)
        {
            _database = redis.GetDatabase();
            _keyPrefix = keyPrefix;
            _jsonOptions = jsonOptions ?? new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        }

        public async Task<TValue> GetAsync(TKey key, CancellationToken cancellationToken = default)
        {
            try
            {
                var redisKey = GetRedisKey(key);
                var value = await _database.StringGetAsync(redisKey);
                return value.HasValue ? JsonSerializer.Deserialize<TValue>(value!, _jsonOptions) ?? default(TValue)! : default(TValue)!;
            }
            catch (Exception ex)
            {
                // Log error and return default
                Console.WriteLine($"[RedisCacheStrategy] Error getting key {key}: {ex.Message}");
                return default(TValue)!;
            }
        }

        public async Task SetAsync(TKey key, TValue value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var redisKey = GetRedisKey(key);
                var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
                await _database.StringSetAsync(redisKey, serializedValue, ttl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RedisCacheStrategy] Error setting key {key}: {ex.Message}");
            }
        }

        public async Task RemoveAsync(TKey key, CancellationToken cancellationToken = default)
        {
            try
            {
                var redisKey = GetRedisKey(key);
                await _database.KeyDeleteAsync(redisKey);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RedisCacheStrategy] Error removing key {key}: {ex.Message}");
            }
        }

        public async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var server = _database.Multiplexer.GetServer(_database.Multiplexer.GetEndPoints()[0]);
                var keys = server.Keys(pattern: $"{_keyPrefix}*");
                foreach (var key in keys)
                {
                    await _database.KeyDeleteAsync(key);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RedisCacheStrategy] Error clearing cache: {ex.Message}");
            }
        }

        public async Task InvalidateAsync(TKey key, CancellationToken cancellationToken = default)
        {
            await RemoveAsync(key, cancellationToken);
        }

        private string GetRedisKey(TKey key)
        {
            return $"{_keyPrefix}{key}";
        }
    }
} 