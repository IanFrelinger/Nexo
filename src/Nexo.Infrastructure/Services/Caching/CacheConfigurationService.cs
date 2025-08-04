using System;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces;
using Nexo.Core.Application.Services;
using Nexo.Shared.Models;

namespace Nexo.Infrastructure.Services.Caching
{
    /// <summary>
    /// Infrastructure implementation of cache configuration service that supports Redis.
    /// </summary>
    public class CacheConfigurationService : Nexo.Core.Application.Services.CacheConfigurationService
    {
        public CacheConfigurationService(CacheSettings settings) : base(settings)
        {
        }

        /// <summary>
        /// Creates a cache strategy based on the current configuration.
        /// </summary>
        /// <typeparam name="TKey">The type of the cache key.</typeparam>
        /// <typeparam name="TValue">The type of the cached value.</typeparam>
        /// <returns>A configured cache strategy instance.</returns>
        public new ICacheStrategy<TKey, TValue> CreateCacheStrategy<TKey, TValue>()
        {
            var backend = GetSettings().Backend?.ToLowerInvariant();
            if (backend == "redis")
            {
                return CreateRedisCacheStrategy<TKey, TValue>();
            }
            else if (backend == "inmemory" || string.IsNullOrEmpty(backend))
            {
                return CreateInMemoryCacheStrategy<TKey, TValue>();
            }
            else
            {
                throw new ArgumentException($"Unsupported cache backend: {GetSettings().Backend}");
            }
        }

        /// <summary>
        /// Creates an in-memory cache strategy with configured TTL.
        /// </summary>
        private ICacheStrategy<TKey, TValue> CreateInMemoryCacheStrategy<TKey, TValue>()
        {
            var settings = GetSettings();
            var ttl = settings.DefaultTtlSeconds > 0 
                ? TimeSpan.FromSeconds(settings.DefaultTtlSeconds) 
                : (TimeSpan?)null;
            
            return new Nexo.Core.Application.Services.CacheStrategy<TKey, TValue>(ttl);
        }

        /// <summary>
        /// Creates a Redis cache strategy with configured connection and settings.
        /// </summary>
        private ICacheStrategy<TKey, TValue> CreateRedisCacheStrategy<TKey, TValue>()
        {
            var settings = GetSettings();
            if (string.IsNullOrEmpty(settings.RedisConnectionString))
            {
                throw new InvalidOperationException("Redis connection string is required when using Redis cache backend.");
            }

            try
            {
                var redis = StackExchange.Redis.ConnectionMultiplexer.Connect(settings.RedisConnectionString);
                return new RedisCacheStrategy<TKey, TValue>(
                    redis, 
                    settings.RedisKeyPrefix ?? "nexo:cache:"
                );
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to connect to Redis: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates the current cache configuration.
        /// </summary>
        /// <returns>True if the configuration is valid, false otherwise.</returns>
        public new async Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var settings = GetSettings();
                if (settings.Backend?.ToLowerInvariant() == "redis")
                {
                    if (string.IsNullOrEmpty(settings.RedisConnectionString))
                    {
                        return false;
                    }

                    // Test Redis connection
                    var redis = StackExchange.Redis.ConnectionMultiplexer.Connect(settings.RedisConnectionString);
                    var database = redis.GetDatabase();
                    await database.PingAsync();
                    return true;
                }

                return true; // In-memory cache is always valid
            }
            catch
            {
                return false;
            }
        }
    }
} 