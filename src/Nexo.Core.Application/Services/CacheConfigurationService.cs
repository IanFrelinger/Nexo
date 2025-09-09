using System;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Application.Services
{
    /// <summary>
    /// Service for managing cache configuration and creating cache strategies.
    /// </summary>
    public class CacheConfigurationService
    {
        private readonly CacheSettings _settings;

        public CacheConfigurationService(CacheSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Creates a cache strategy based on the current configuration.
        /// </summary>
        /// <typeparam name="TKey">The type of the cache key.</typeparam>
        /// <typeparam name="TValue">The type of the cached value.</typeparam>
        /// <returns>A configured cache strategy instance.</returns>
        public ICacheStrategy<TKey, TValue> CreateCacheStrategy<TKey, TValue>() where TKey : notnull
        {
            var backend = _settings.Backend?.ToLowerInvariant();
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
                throw new ArgumentException($"Unsupported cache backend: {_settings.Backend}");
            }
        }

        /// <summary>
        /// Creates an in-memory cache strategy with configured TTL.
        /// </summary>
        private ICacheStrategy<TKey, TValue> CreateInMemoryCacheStrategy<TKey, TValue>() where TKey : notnull
        {
            var ttl = _settings.DefaultTtlSeconds > 0 
                ? TimeSpan.FromSeconds(_settings.DefaultTtlSeconds) 
                : (TimeSpan?)null;
            
            return new CacheStrategy<TKey, TValue>(ttl);
        }

        /// <summary>
        /// Creates a Redis cache strategy with configured connection and settings.
        /// Note: This method throws NotImplementedException as Redis implementation is in Infrastructure layer.
        /// </summary>
        private ICacheStrategy<TKey, TValue> CreateRedisCacheStrategy<TKey, TValue>()
        {
            throw new NotImplementedException("Redis cache strategy is implemented in the Infrastructure layer. Use CacheConfigurationService from Infrastructure instead.");
        }

        /// <summary>
        /// Gets the current cache settings.
        /// </summary>
        public CacheSettings GetSettings() => _settings;

        /// <summary>
        /// Validates the current cache configuration.
        /// </summary>
        /// <returns>True if the configuration is valid, false otherwise.</returns>
        public Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (_settings.Backend?.ToLowerInvariant() == "redis")
                {
                    // Redis validation is handled in Infrastructure layer
                    return Task.FromResult(false); // Indicate that Redis validation is not available in this layer
                }

                return Task.FromResult(true); // In-memory cache is always valid
            }
            catch
            {
                return Task.FromResult(false);
            }
        }
    }
} 