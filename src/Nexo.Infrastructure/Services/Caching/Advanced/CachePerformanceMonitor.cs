using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces.Caching;

namespace Nexo.Infrastructure.Services.Caching.Advanced
{
    /// <summary>
    /// Advanced cache performance monitoring service for Phase 3.3.
    /// </summary>
    public class CachePerformanceMonitor : ICachePerformanceMonitor
    {
        private readonly ConcurrentQueue<CacheOperation> _operations;
        private readonly object _lock = new object();
        private bool _disposed = false;

        public CachePerformanceMonitor()
        {
            _operations = new ConcurrentQueue<CacheOperation>();
        }

        /// <summary>
        /// Records a cache operation for performance tracking.
        /// </summary>
        public void RecordOperation(CacheOperation operation)
        {
            if (_disposed) return;

            _operations.Enqueue(operation);

            // Keep only recent operations to prevent memory growth
            while (_operations.Count > 10000)
            {
                _operations.TryDequeue(out _);
            }
        }

        /// <summary>
        /// Generates a comprehensive performance report.
        /// </summary>
        public async Task<CachePerformanceReport> GenerateReportAsync(CancellationToken cancellationToken = default)
        {
            await Task.Yield(); // Simulate async operation

            var operations = _operations.ToArray();
            if (!operations.Any())
            {
                return new CachePerformanceReport
                {
                    GeneratedAt = DateTimeOffset.UtcNow,
                    TotalOperations = 0
                };
            }

            var report = new CachePerformanceReport
            {
                GeneratedAt = DateTimeOffset.UtcNow,
                TotalOperations = operations.Length,
                HitRate = CalculateHitRate(operations),
                AverageResponseTime = CalculateAverageResponseTime(operations),
                ErrorRate = CalculateErrorRate(operations),
                OperationsByType = GroupOperationsByType(operations),
                PerformanceMetrics = CalculatePerformanceMetrics(operations),
                Recommendations = await GenerateRecommendationsAsync(operations, cancellationToken)
            };

            return report;
        }

        /// <summary>
        /// Gets optimization recommendations based on current performance metrics.
        /// </summary>
        public async Task<IEnumerable<CacheOptimizationRecommendation>> GetOptimizationRecommendationsAsync(
            CancellationToken cancellationToken = default)
        {
            await Task.Yield(); // Simulate async operation

            var operations = _operations.ToArray();
            var recommendations = new List<CacheOptimizationRecommendation>();

            // Analyze hit rate
            var hitRate = CalculateHitRate(operations);
            if (hitRate < 0.7)
            {
                recommendations.Add(new CacheOptimizationRecommendation
                {
                    Type = OptimizationType.LowHitRate,
                    Priority = RecommendationPriority.High,
                    Title = "Low Cache Hit Rate",
                    Description = $"Current hit rate is {hitRate:P1}. Consider increasing cache size or TTL.",
                    Impact = "High",
                    Effort = "Medium"
                });
            }

            // Analyze response times
            var avgResponseTime = CalculateAverageResponseTime(operations);
            if (avgResponseTime > TimeSpan.FromMilliseconds(100))
            {
                recommendations.Add(new CacheOptimizationRecommendation
                {
                    Type = OptimizationType.SlowResponse,
                    Priority = RecommendationPriority.Medium,
                    Title = "Slow Cache Response Times",
                    Description = $"Average response time is {avgResponseTime.TotalMilliseconds:F1}ms. Consider optimizing cache backend.",
                    Impact = "Medium",
                    Effort = "High"
                });
            }

            // Analyze error rate
            var errorRate = CalculateErrorRate(operations);
            if (errorRate > 0.05)
            {
                recommendations.Add(new CacheOptimizationRecommendation
                {
                    Type = OptimizationType.HighErrorRate,
                    Priority = RecommendationPriority.High,
                    Title = "High Error Rate",
                    Description = $"Error rate is {errorRate:P1}. Check cache backend health and configuration.",
                    Impact = "High",
                    Effort = "Low"
                });
            }

            return recommendations;
        }

        private double CalculateHitRate(CacheOperation[] operations)
        {
            var getOperations = operations.Where(o => o.OperationType == CacheOperationType.Get).ToArray();
            if (!getOperations.Any()) return 0.0;

            var hits = getOperations.Count(o => o.Hit);
            return (double)hits / getOperations.Length;
        }

        private TimeSpan CalculateAverageResponseTime(CacheOperation[] operations)
        {
            var operationsWithDuration = operations.Where(o => o.Duration.HasValue).ToArray();
            if (!operationsWithDuration.Any()) return TimeSpan.Zero;

            var totalDuration = operationsWithDuration.Sum(o => o.Duration!.Value);
            return TimeSpan.FromTicks(totalDuration.Ticks / operationsWithDuration.Length);
        }

        private double CalculateErrorRate(CacheOperation[] operations)
        {
            if (!operations.Any()) return 0.0;

            var errors = operations.Count(o => !o.Success);
            return (double)errors / operations.Length;
        }

        private Dictionary<CacheOperationType, int> GroupOperationsByType(CacheOperation[] operations)
        {
            return operations
                .GroupBy(o => o.OperationType)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        private CachePerformanceMetrics CalculatePerformanceMetrics(CacheOperation[] operations)
        {
            var getOperations = operations.Where(o => o.OperationType == CacheOperationType.Get).ToArray();
            var setOperations = operations.Where(o => o.OperationType == CacheOperationType.Set).ToArray();

            return new CachePerformanceMetrics
            {
                GetOperations = getOperations.Length,
                SetOperations = setOperations.Length,
                HitCount = getOperations.Count(o => o.Hit),
                MissCount = getOperations.Count(o => !o.Hit),
                ErrorCount = operations.Count(o => !o.Success),
                AverageGetTime = CalculateAverageTime(getOperations),
                AverageSetTime = CalculateAverageTime(setOperations),
                P95ResponseTime = CalculatePercentile(operations, 95),
                P99ResponseTime = CalculatePercentile(operations, 99)
            };
        }

        private TimeSpan CalculateAverageTime(CacheOperation[] operations)
        {
            var operationsWithDuration = operations.Where(o => o.Duration.HasValue).ToArray();
            if (!operationsWithDuration.Any()) return TimeSpan.Zero;

            var totalDuration = operationsWithDuration.Sum(o => o.Duration!.Value);
            return TimeSpan.FromTicks(totalDuration.Ticks / operationsWithDuration.Length);
        }

        private TimeSpan CalculatePercentile(CacheOperation[] operations, int percentile)
        {
            var operationsWithDuration = operations
                .Where(o => o.Duration.HasValue)
                .OrderBy(o => o.Duration!.Value)
                .ToArray();

            if (!operationsWithDuration.Any()) return TimeSpan.Zero;

            var index = (int)Math.Ceiling(operationsWithDuration.Length * percentile / 100.0) - 1;
            index = Math.Max(0, Math.Min(index, operationsWithDuration.Length - 1));

            return operationsWithDuration[index].Duration!.Value;
        }

        private async Task<List<string>> GenerateRecommendationsAsync(CacheOperation[] operations, CancellationToken cancellationToken)
        {
            await Task.Yield(); // Simulate async operation

            var recommendations = new List<string>();

            var hitRate = CalculateHitRate(operations);
            if (hitRate < 0.8)
            {
                recommendations.Add("Consider increasing cache TTL or implementing more intelligent caching strategies");
            }

            var avgResponseTime = CalculateAverageResponseTime(operations);
            if (avgResponseTime > TimeSpan.FromMilliseconds(50))
            {
                recommendations.Add("Consider optimizing cache backend or implementing cache warming strategies");
            }

            var errorRate = CalculateErrorRate(operations);
            if (errorRate > 0.01)
            {
                recommendations.Add("Investigate and resolve cache backend errors");
            }

            return recommendations;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                // Clean up resources if needed
            }
        }
    }

    /// <summary>
    /// Cache performance report model.
    /// </summary>
    public class CachePerformanceReport
    {
        public DateTimeOffset GeneratedAt { get; set; }
        public int TotalOperations { get; set; }
        public double HitRate { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public double ErrorRate { get; set; }
        public Dictionary<CacheOperationType, int> OperationsByType { get; set; } = new Dictionary<CacheOperationType, int>();
        public CachePerformanceMetrics PerformanceMetrics { get; set; } = new CachePerformanceMetrics();
        public List<string> Recommendations { get; set; } = new List<string>();
    }

    /// <summary>
    /// Cache performance metrics.
    /// </summary>
    public class CachePerformanceMetrics
    {
        public int GetOperations { get; set; }
        public int SetOperations { get; set; }
        public int HitCount { get; set; }
        public int MissCount { get; set; }
        public int ErrorCount { get; set; }
        public TimeSpan AverageGetTime { get; set; }
        public TimeSpan AverageSetTime { get; set; }
        public TimeSpan P95ResponseTime { get; set; }
        public TimeSpan P99ResponseTime { get; set; }
    }

    /// <summary>
    /// Cache optimization recommendation.
    /// </summary>
    public class CacheOptimizationRecommendation
    {
        public OptimizationType Type { get; set; }
        public RecommendationPriority Priority { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
        public string Effort { get; set; } = string.Empty;
    }

    /// <summary>
    /// Optimization types.
    /// </summary>
    public enum OptimizationType
    {
        LowHitRate,
        SlowResponse,
        HighErrorRate,
        MemoryUsage,
        Configuration
    }

    /// <summary>
    /// Recommendation priority levels.
    /// </summary>
    public enum RecommendationPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
}