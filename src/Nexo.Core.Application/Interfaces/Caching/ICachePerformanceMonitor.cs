using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces.Caching
{
    /// <summary>
    /// Interface for cache performance monitoring and analytics.
    /// Part of Phase 3.3 advanced caching features.
    /// </summary>
    public interface ICachePerformanceMonitor : IDisposable
    {
        /// <summary>
        /// Records a cache operation for performance tracking.
        /// </summary>
        /// <param name="operation">The cache operation to record.</param>
        void RecordOperation(CacheOperation operation);

        /// <summary>
        /// Generates a comprehensive performance report.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A detailed performance report.</returns>
        Task<CachePerformanceReport> GenerateReportAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets optimization recommendations based on current performance metrics.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of optimization recommendations.</returns>
        Task<IEnumerable<CacheOptimizationRecommendation>> GetOptimizationRecommendationsAsync(
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Cache operation tracking model.
    /// </summary>
    public class CacheOperation
    {
        public string CacheName { get; set; } = string.Empty;
        public CacheOperationType OperationType { get; set; }
        public string Key { get; set; } = string.Empty;
        public bool Success { get; set; }
        public bool Hit { get; set; }
        public TimeSpan? Duration { get; set; }
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Cache operation types.
    /// </summary>
    public enum CacheOperationType
    {
        Get,
        Set,
        Remove,
        Clear,
        Refresh
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