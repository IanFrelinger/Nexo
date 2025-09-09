using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces.Performance
{
    /// <summary>
    /// Interface for production-ready performance optimization and benchmarking.
    /// Part of Phase 3.4 production readiness features.
    /// </summary>
    public interface IProductionPerformanceOptimizer
    {
        /// <summary>
        /// Runs comprehensive performance optimization across all services.
        /// </summary>
        /// <param name="options">Optimization options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Performance optimization result.</returns>
        Task<PerformanceOptimizationResult> OptimizePerformanceAsync(
            PerformanceOptimizationOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Runs comprehensive performance benchmarking.
        /// </summary>
        /// <param name="options">Benchmark options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Performance benchmark result.</returns>
        Task<PerformanceBenchmarkResult> RunBenchmarkAsync(
            PerformanceBenchmarkOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets performance recommendations based on current metrics.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of performance recommendations.</returns>
        Task<IEnumerable<PerformanceRecommendation>> GetPerformanceRecommendationsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets performance trends over time.
        /// </summary>
        /// <param name="timeWindow">Time window for trend analysis.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Performance trends.</returns>
        Task<PerformanceTrends> GetPerformanceTrendsAsync(
            TimeSpan timeWindow,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Performance optimization options.
    /// </summary>
    public class PerformanceOptimizationOptions
    {
        public bool OptimizeCaching { get; set; } = true;
        public bool OptimizeMemory { get; set; } = true;
        public bool OptimizeAI { get; set; } = true;
        public bool OptimizeSecurity { get; set; } = true;
        public bool OptimizeDatabase { get; set; } = true;
        public bool OptimizeNetwork { get; set; } = true;
        public TimeSpan MaxOptimizationTime { get; set; } = TimeSpan.FromMinutes(10);
    }

    /// <summary>
    /// Performance benchmark options.
    /// </summary>
    public class PerformanceBenchmarkOptions
    {
        public string BenchmarkName { get; set; } = "Default";
        public int Iterations { get; set; } = 10;
        public TimeSpan WarmupTime { get; set; } = TimeSpan.FromSeconds(30);
        public bool IncludeSystemMetrics { get; set; } = true;
        public bool IncludeCacheMetrics { get; set; } = true;
        public bool IncludeAIMetrics { get; set; } = true;
        public bool IncludeSecurityMetrics { get; set; } = true;
        public bool IncludeDatabaseMetrics { get; set; } = true;
        public bool IncludeEndToEndMetrics { get; set; } = true;
    }

    /// <summary>
    /// Performance optimization result.
    /// </summary>
    public class PerformanceOptimizationResult
    {
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public TimeSpan TotalOptimizationTime => EndTime - StartTime;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public PerformanceOptimizationOptions Options { get; set; } = new();
        
        public CacheOptimizationResult? CacheOptimization { get; set; }
        public MemoryOptimizationResult? MemoryOptimization { get; set; }
        public AIOptimizationResult? AIOptimization { get; set; }
        public SecurityOptimizationResult? SecurityOptimization { get; set; }
        public DatabaseOptimizationResult? DatabaseOptimization { get; set; }
        public NetworkOptimizationResult? NetworkOptimization { get; set; }

        public int GetTotalImprovements()
        {
            var improvements = 0;
            if (CacheOptimization?.Success == true) improvements++;
            if (MemoryOptimization?.Success == true) improvements++;
            if (AIOptimization?.Success == true) improvements++;
            if (SecurityOptimization?.Success == true) improvements++;
            if (DatabaseOptimization?.Success == true) improvements++;
            if (NetworkOptimization?.Success == true) improvements++;
            return improvements;
        }
    }

    /// <summary>
    /// Performance benchmark result.
    /// </summary>
    public class PerformanceBenchmarkResult
    {
        public PerformanceBenchmark Benchmark { get; set; } = new();
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Performance benchmark data.
    /// </summary>
    public class PerformanceBenchmark
    {
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public PerformanceBenchmarkOptions Options { get; set; } = new();
        
        public SystemResourceMetrics? SystemMetrics { get; set; }
        public CachePerformanceMetrics? CacheMetrics { get; set; }
        public AIPerformanceMetrics? AIMetrics { get; set; }
        public SecurityPerformanceMetrics? SecurityMetrics { get; set; }
        public DatabasePerformanceMetrics? DatabaseMetrics { get; set; }
        public EndToEndPerformanceMetrics? EndToEndMetrics { get; set; }
    }

    /// <summary>
    /// Performance recommendation.
    /// </summary>
    public class PerformanceRecommendation
    {
        public string Category { get; set; } = string.Empty;
        public PerformancePriority Priority { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string EstimatedImpact { get; set; } = string.Empty;
        public string ImplementationEffort { get; set; } = string.Empty;
    }

    /// <summary>
    /// Performance trends over time.
    /// </summary>
    public class PerformanceTrends
    {
        public TimeSpan TimeWindow { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public PerformanceTrend CacheHitRateTrend { get; set; }
        public PerformanceTrend AIResponseTimeTrend { get; set; }
        public PerformanceTrend MemoryUsageTrend { get; set; }
        public PerformanceTrend OverallPerformanceTrend { get; set; }
    }

    /// <summary>
    /// Performance trend direction.
    /// </summary>
    public enum PerformanceTrend
    {
        Improving,
        Stable,
        Degrading
    }

    /// <summary>
    /// Performance priority level.
    /// </summary>
    public enum PerformancePriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    #region Optimization Results

    /// <summary>
    /// Cache optimization result.
    /// </summary>
    public class CacheOptimizationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public double ImprovementPercentage { get; set; }
        public List<string> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// Memory optimization result.
    /// </summary>
    public class MemoryOptimizationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public long MemorySavedMB { get; set; }
        public double ImprovementPercentage { get; set; }
    }

    /// <summary>
    /// AI optimization result.
    /// </summary>
    public class AIOptimizationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public double ResponseTimeImprovement { get; set; }
        public double AccuracyImprovement { get; set; }
    }

    /// <summary>
    /// Security optimization result.
    /// </summary>
    public class SecurityOptimizationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public double SecurityCheckTimeImprovement { get; set; }
        public double EncryptionTimeImprovement { get; set; }
    }

    /// <summary>
    /// Database optimization result.
    /// </summary>
    public class DatabaseOptimizationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public double QueryTimeImprovement { get; set; }
        public double ConnectionPoolEfficiencyImprovement { get; set; }
    }

    /// <summary>
    /// Network optimization result.
    /// </summary>
    public class NetworkOptimizationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public double NetworkLatencyImprovement { get; set; }
        public double ThroughputImprovement { get; set; }
    }

    #endregion

    #region Performance Metrics

    /// <summary>
    /// System resource metrics.
    /// </summary>
    public class SystemResourceMetrics
    {
        public double CPUUsagePercent { get; set; }
        public long MemoryUsageMB { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }
        public TimeSpan Uptime { get; set; }
    }

    /// <summary>
    /// Cache performance metrics.
    /// </summary>
    public class CachePerformanceMetrics
    {
        public double HitRate { get; set; }
        public double MissRate { get; set; }
        public double EvictionRate { get; set; }
        public TimeSpan AverageAccessTime { get; set; }
        public long TotalRequests { get; set; }
        public long CacheSize { get; set; }
    }

    /// <summary>
    /// AI performance metrics.
    /// </summary>
    public class AIPerformanceMetrics
    {
        public TimeSpan AverageResponseTime { get; set; }
        public double RequestsPerSecond { get; set; }
        public double SuccessRate { get; set; }
        public double Accuracy { get; set; }
        public long TotalTokens { get; set; }
        public double CostPerRequest { get; set; }
    }

    /// <summary>
    /// Security performance metrics.
    /// </summary>
    public class SecurityPerformanceMetrics
    {
        public TimeSpan AverageSecurityCheckTime { get; set; }
        public TimeSpan EncryptionTime { get; set; }
        public TimeSpan AuditLogTime { get; set; }
        public double SecurityCheckSuccessRate { get; set; }
        public long TotalSecurityEvents { get; set; }
    }

    /// <summary>
    /// Database performance metrics.
    /// </summary>
    public class DatabasePerformanceMetrics
    {
        public TimeSpan AverageQueryTime { get; set; }
        public double ConnectionPoolUtilization { get; set; }
        public TimeSpan TransactionTime { get; set; }
        public long TotalQueries { get; set; }
        public double QuerySuccessRate { get; set; }
    }

    /// <summary>
    /// End-to-end performance metrics.
    /// </summary>
    public class EndToEndPerformanceMetrics
    {
        public TimeSpan TotalWorkflowTime { get; set; }
        public double Throughput { get; set; }
        public double ErrorRate { get; set; }
        public double SuccessRate { get; set; }
        public long TotalRequests { get; set; }
    }

    #endregion
}
