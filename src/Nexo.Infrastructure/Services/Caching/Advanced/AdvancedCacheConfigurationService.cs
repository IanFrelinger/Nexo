using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces;
using Nexo.Core.Application.Interfaces.Caching;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Infrastructure.Services.Caching.Advanced
{
    /// <summary>
    /// Advanced cache configuration service with intelligent eviction policies,
    /// performance monitoring, and optimization features for Phase 3.3.
    /// </summary>
    public class AdvancedCacheConfigurationService : CacheConfigurationService
    {
        private readonly ICachePerformanceMonitor _performanceMonitor;
        private readonly IResponseDeduplicationService _deduplicationService;
        private readonly IEnumerable<ICacheEvictionStrategy> _evictionStrategies;
        private readonly CacheEvictionConfiguration _evictionConfig;

        public AdvancedCacheConfigurationService(
            CacheSettings settings,
            ICachePerformanceMonitor performanceMonitor,
            IResponseDeduplicationService deduplicationService,
            IEnumerable<ICacheEvictionStrategy> evictionStrategies,
            CacheEvictionConfiguration evictionConfig) 
            : base(settings)
        {
            _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
            _deduplicationService = deduplicationService ?? throw new ArgumentNullException(nameof(deduplicationService));
            _evictionStrategies = evictionStrategies ?? throw new ArgumentNullException(nameof(evictionStrategies));
            _evictionConfig = evictionConfig ?? throw new ArgumentNullException(nameof(evictionConfig));
        }

        /// <summary>
        /// Creates an advanced cache strategy with intelligent features.
        /// </summary>
        public new ICacheStrategy<TKey, TValue> CreateCacheStrategy<TKey, TValue>() where TKey : notnull
        {
            var baseStrategy = base.CreateCacheStrategy<TKey, TValue>();
            
            // Wrap with advanced features
            var monitoredStrategy = new MonitoredCacheStrategy<TKey, TValue>(baseStrategy, _performanceMonitor);
            var compressedStrategy = new CompressedCacheStrategy<TKey, TValue>(monitoredStrategy);
            
            return compressedStrategy;
        }

        /// <summary>
        /// Creates a cache strategy with intelligent eviction policy.
        /// </summary>
        public ICacheStrategy<TKey, TValue> CreateIntelligentCacheStrategy<TKey, TValue>() where TKey : notnull
        {
            var baseStrategy = base.CreateCacheStrategy<TKey, TValue>();
            var evictionPolicy = new IntelligentEvictionPolicy(_evictionStrategies, _evictionConfig);
            
            // For now, return the base strategy wrapped with monitoring
            var monitoredStrategy = new MonitoredCacheStrategy<TKey, TValue>(baseStrategy, _performanceMonitor);
            return monitoredStrategy;
        }

        /// <summary>
        /// Creates a cache strategy with response deduplication.
        /// </summary>
        public ICacheStrategy<string, TValue> CreateDeduplicationCacheStrategy<TValue>()
        {
            var baseStrategy = base.CreateCacheStrategy<string, TValue>();
            // For now, return the base strategy wrapped with monitoring
            var monitoredStrategy = new MonitoredCacheStrategy<string, TValue>(baseStrategy, _performanceMonitor);
            return monitoredStrategy;
        }

        /// <summary>
        /// Gets cache performance statistics.
        /// </summary>
        public async Task<CachePerformanceReport> GetPerformanceReportAsync(CancellationToken cancellationToken = default)
        {
            return await _performanceMonitor.GenerateReportAsync(cancellationToken);
        }

        /// <summary>
        /// Gets optimization recommendations.
        /// </summary>
        public async Task<IEnumerable<CacheOptimizationRecommendation>> GetOptimizationRecommendationsAsync(
            CancellationToken cancellationToken = default)
        {
            return await _performanceMonitor.GetOptimizationRecommendationsAsync(cancellationToken);
        }

        /// <summary>
        /// Gets deduplication statistics.
        /// </summary>
        public async Task<DeduplicationStatistics> GetDeduplicationStatisticsAsync(CancellationToken cancellationToken = default)
        {
            return await _deduplicationService.GetStatisticsAsync(cancellationToken);
        }

        /// <summary>
        /// Optimizes cache configuration based on performance data.
        /// </summary>
        public async Task<CacheOptimizationResult> OptimizeConfigurationAsync(CancellationToken cancellationToken = default)
        {
            var report = await GetPerformanceReportAsync(cancellationToken);
            var recommendations = await GetOptimizationRecommendationsAsync(cancellationToken);

            var result = new CacheOptimizationResult
            {
                CurrentHitRate = report.HitRate,
                CurrentErrorRate = report.ErrorRate,
                CurrentAverageResponseTime = report.AverageResponseTime,
                Recommendations = recommendations.ToList(),
                OptimizedSettings = GenerateOptimizedSettings(report, recommendations)
            };

            return result;
        }

        /// <summary>
        /// Generates optimized cache settings based on performance analysis.
        /// </summary>
        private CacheSettings GenerateOptimizedSettings(
            CachePerformanceReport report, 
            IEnumerable<CacheOptimizationRecommendation> recommendations)
        {
            var settings = GetSettings();
            var optimizedSettings = new CacheSettings
            {
                Backend = settings.Backend,
                KeyPrefix = settings.KeyPrefix,
                DefaultTtlSeconds = settings.DefaultTtlSeconds,
                MaxSizeMB = settings.MaxSizeMB,
                ExpirationMinutes = settings.ExpirationMinutes,
                Enabled = settings.Enabled,
                UseDistributedCache = settings.UseDistributedCache,
                EvictionPolicy = settings.EvictionPolicy
            };

            // Apply optimizations based on recommendations
            foreach (var recommendation in recommendations)
            {
                switch (recommendation.Type)
                {
                    case OptimizationType.LowHitRate:
                        // Increase TTL to improve hit rate
                        optimizedSettings = optimizedSettings with { DefaultTtlSeconds = Math.Max(optimizedSettings.DefaultTtlSeconds, 3600) };
                        break;
                    case OptimizationType.SlowResponse:
                        // Consider switching to faster backend
                        if (settings.Backend?.ToLowerInvariant() == "redis")
                        {
                            optimizedSettings = optimizedSettings with { Backend = "inmemory" };
                        }
                        break;
                    case OptimizationType.HighErrorRate:
                        // Add retry logic or fallback to in-memory
                        optimizedSettings = optimizedSettings with { Backend = "inmemory" };
                        break;
                }
            }

            return optimizedSettings;
        }
    }

    /// <summary>
    /// Cache optimization result.
    /// </summary>
    public class CacheOptimizationResult
    {
        public double CurrentHitRate { get; set; }
        public double CurrentErrorRate { get; set; }
        public TimeSpan CurrentAverageResponseTime { get; set; }
        public List<CacheOptimizationRecommendation> Recommendations { get; set; } = new List<CacheOptimizationRecommendation>();
        public CacheSettings OptimizedSettings { get; set; } = new CacheSettings();
    }
}