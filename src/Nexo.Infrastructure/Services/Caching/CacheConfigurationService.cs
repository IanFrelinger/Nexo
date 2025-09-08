using System;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces;
using Nexo.Shared.Models;

namespace Nexo.Infrastructure.Services.Caching
{
    /// <summary>
    /// Implementation of the cache configuration service, extending core application functionality
    /// with support for Redis and other configurable backend caching mechanisms.
    /// </summary>
    public class CacheConfigurationService : Nexo.Core.Application.Services.CacheConfigurationService
    {
        /// <summary>
        /// Provides an implementation of a cache configuration service supporting
        /// various caching backends such as Redis or in-memory caching.
        /// </summary>
        public CacheConfigurationService(CacheSettings settings) : base(settings)
        {
        }

        /// <summary>
        /// Creates a cache strategy based on the current configuration.
        /// </summary>
        /// <typeparam name="TKey">The type of the cache key.</typeparam>
        /// <typeparam name="TValue">The type of the cached value.</typeparam>
        /// <returns>A configured cache strategy instance.</returns>
        public new ICacheStrategy<TKey, TValue> CreateCacheStrategy<TKey, TValue>() where TKey : notnull
        {
            var backend = GetSettings().Backend?.ToLowerInvariant();
            if (backend == "redis")
            {
                return CreateRedisCacheStrategy<TKey, TValue>();
            }

            if (backend == "inmemory" || string.IsNullOrEmpty(backend))
            {
                return CreateInMemoryCacheStrategy<TKey, TValue>();
            }

            throw new ArgumentException($"Unsupported cache backend: {GetSettings().Backend}");
        }

        /// <summary>
        /// Creates an in-memory cache strategy with a configurable time-to-live (TTL).
        /// </summary>
        /// <typeparam name="TKey">The type of the cache key.</typeparam>
        /// <typeparam name="TValue">The type of the cached value.</typeparam>
        /// <returns>An in-memory cache strategy configured with the specified TTL.</returns>
        private ICacheStrategy<TKey, TValue> CreateInMemoryCacheStrategy<TKey, TValue>() where TKey : notnull
        {
            var settings = GetSettings();
            var ttl = settings.DefaultTtlSeconds > 0 
                ? TimeSpan.FromSeconds(settings.DefaultTtlSeconds) 
                : (TimeSpan?)null;
            
            return new Nexo.Core.Application.Services.CacheStrategy<TKey, TValue>(ttl);
        }

        /// <summary>
        /// Creates a Redis cache strategy with the specified connection and settings.
        /// </summary>
        /// <typeparam name="TKey">The type of the cache key.</typeparam>
        /// <typeparam name="TValue">The type of the cached value.</typeparam>
        /// <returns>A Redis-based cache strategy instance configured with the given settings.</returns>
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
        /// Validates the current cache configuration asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is true if the configuration is valid; otherwise, false.</returns>
        public new async Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var settings = GetSettings();
                if (settings.Backend?.ToLowerInvariant() != "redis") return true; // In-memory cache is always valid
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
            catch
            {
                return false;
            }
        }
    }
} 