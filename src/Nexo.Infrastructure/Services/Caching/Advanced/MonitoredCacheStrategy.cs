using System;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces;
using Nexo.Core.Application.Interfaces.Caching;

namespace Nexo.Infrastructure.Services.Caching.Advanced
{
    /// <summary>
    /// Cache strategy that monitors performance and records metrics.
    /// Part of Phase 3.3 advanced caching features.
    /// </summary>
    public class MonitoredCacheStrategy<TKey, TValue> : ICacheStrategy<TKey, TValue> where TKey : notnull
    {
        private readonly ICacheStrategy<TKey, TValue> _innerStrategy;
        private readonly ICachePerformanceMonitor _performanceMonitor;

        public MonitoredCacheStrategy(ICacheStrategy<TKey, TValue> innerStrategy, ICachePerformanceMonitor performanceMonitor)
        {
            _innerStrategy = innerStrategy ?? throw new ArgumentNullException(nameof(innerStrategy));
            _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
        }

        /// <summary>
        /// Gets a value from the cache with performance monitoring.
        /// </summary>
        public async Task<TValue> GetAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var startTime = DateTimeOffset.UtcNow;
            var operation = new CacheOperation
            {
                CacheName = typeof(TValue).Name,
                OperationType = CacheOperationType.Get,
                Key = key.ToString() ?? string.Empty,
                Timestamp = startTime
            };

            try
            {
                var result = await _innerStrategy.GetAsync(key, cancellationToken);
                var duration = DateTimeOffset.UtcNow - startTime;

                operation.Success = true;
                operation.Hit = !IsDefaultValue(result);
                operation.Duration = duration;

                _performanceMonitor.RecordOperation(operation);
                return result;
            }
            catch (Exception ex)
            {
                var duration = DateTimeOffset.UtcNow - startTime;

                operation.Success = false;
                operation.ErrorMessage = ex.Message;
                operation.Duration = duration;

                _performanceMonitor.RecordOperation(operation);
                throw;
            }
        }

        /// <summary>
        /// Sets a value in the cache with performance monitoring.
        /// </summary>
        public async Task SetAsync(TKey key, TValue value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
        {
            var startTime = DateTimeOffset.UtcNow;
            var operation = new CacheOperation
            {
                CacheName = typeof(TValue).Name,
                OperationType = CacheOperationType.Set,
                Key = key.ToString() ?? string.Empty,
                Timestamp = startTime
            };

            try
            {
                await _innerStrategy.SetAsync(key, value, ttl, cancellationToken);
                var duration = DateTimeOffset.UtcNow - startTime;

                operation.Success = true;
                operation.Duration = duration;

                _performanceMonitor.RecordOperation(operation);
            }
            catch (Exception ex)
            {
                var duration = DateTimeOffset.UtcNow - startTime;

                operation.Success = false;
                operation.ErrorMessage = ex.Message;
                operation.Duration = duration;

                _performanceMonitor.RecordOperation(operation);
                throw;
            }
        }

        /// <summary>
        /// Removes a value from the cache with performance monitoring.
        /// </summary>
        public async Task RemoveAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var startTime = DateTimeOffset.UtcNow;
            var operation = new CacheOperation
            {
                CacheName = typeof(TValue).Name,
                OperationType = CacheOperationType.Remove,
                Key = key.ToString() ?? string.Empty,
                Timestamp = startTime
            };

            try
            {
                await _innerStrategy.RemoveAsync(key, cancellationToken);
                var duration = DateTimeOffset.UtcNow - startTime;

                operation.Success = true;
                operation.Duration = duration;

                _performanceMonitor.RecordOperation(operation);
            }
            catch (Exception ex)
            {
                var duration = DateTimeOffset.UtcNow - startTime;

                operation.Success = false;
                operation.ErrorMessage = ex.Message;
                operation.Duration = duration;

                _performanceMonitor.RecordOperation(operation);
                throw;
            }
        }

        /// <summary>
        /// Clears all entries from the cache with performance monitoring.
        /// </summary>
        public async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            var startTime = DateTimeOffset.UtcNow;
            var operation = new CacheOperation
            {
                CacheName = typeof(TValue).Name,
                OperationType = CacheOperationType.Clear,
                Key = "ALL",
                Timestamp = startTime
            };

            try
            {
                await _innerStrategy.ClearAsync(cancellationToken);
                var duration = DateTimeOffset.UtcNow - startTime;

                operation.Success = true;
                operation.Duration = duration;

                _performanceMonitor.RecordOperation(operation);
            }
            catch (Exception ex)
            {
                var duration = DateTimeOffset.UtcNow - startTime;

                operation.Success = false;
                operation.ErrorMessage = ex.Message;
                operation.Duration = duration;

                _performanceMonitor.RecordOperation(operation);
                throw;
            }
        }

        /// <summary>
        /// Invalidates a cache entry with performance monitoring.
        /// </summary>
        public async Task InvalidateAsync(TKey key, CancellationToken cancellationToken = default)
        {
            await RemoveAsync(key, cancellationToken);
        }

        /// <summary>
        /// Checks if a value is the default value for the type.
        /// </summary>
        private static bool IsDefaultValue(TValue value)
        {
            return value == null || value.Equals(default(TValue));
        }
    }
}