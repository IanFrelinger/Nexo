using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Interfaces
{
    /// <summary>
    /// Interface for parallel processing optimizer that provides intelligent parallel processing strategies and resource optimization.
    /// </summary>
    public interface IParallelProcessingOptimizer
    {
        /// <summary>
        /// Determines the optimal processing strategy for a set of requests.
        /// </summary>
        /// <param name="requests">The requests to process.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The optimal processing strategy.</returns>
        Task<ProcessingStrategy> DetermineOptimalStrategyAsync(IEnumerable<ModelRequest> requests, CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes requests in parallel using the specified strategy.
        /// </summary>
        /// <param name="requests">The requests to process.</param>
        /// <param name="strategy">The processing strategy to use.</param>
        /// <param name="processor">The processor function for individual requests.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The processed responses.</returns>
        Task<IEnumerable<ModelResponse>> ProcessInParallelAsync(
            IEnumerable<ModelRequest> requests,
            ProcessingStrategy strategy,
            Func<ModelRequest, CancellationToken, Task<ModelResponse>> processor,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Optimizes processing based on collected metrics.
        /// </summary>
        /// <param name="metrics">The processing metrics to analyze.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Processing optimization recommendations.</returns>
        Task<ProcessingOptimization> OptimizeProcessingAsync(IEnumerable<ProcessingMetrics> metrics, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets performance metrics for processing operations.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Processing performance metrics.</returns>
        Task<ProcessingPerformance> GetPerformanceMetricsAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Strategy for processing requests in parallel.
    /// </summary>
    public class ProcessingStrategy
    {
        /// <summary>
        /// Gets or sets the maximum number of parallel operations.
        /// </summary>
        public int MaxParallelism { get; set; }

        /// <summary>
        /// Gets or sets the processing order of requests.
        /// </summary>
        public List<ModelRequest> ProcessingOrder { get; set; } = new List<ModelRequest>();

        /// <summary>
        /// Gets or sets the batch size for processing.
        /// </summary>
        public int BatchSize { get; set; }

        /// <summary>
        /// Gets or sets the resource allocation strategy.
        /// </summary>
        public ResourceAllocation ResourceAllocation { get; set; } = new ResourceAllocation();

        /// <summary>
        /// Gets or sets the estimated processing duration.
        /// </summary>
        public TimeSpan EstimatedDuration { get; set; }

        /// <summary>
        /// Gets or sets the priority level for processing.
        /// </summary>
        public PriorityLevel PriorityLevel { get; set; }
    }

    /// <summary>
    /// Resource allocation configuration for processing.
    /// </summary>
    public class ResourceAllocation
    {
        /// <summary>
        /// Gets or sets the maximum CPU percentage to use.
        /// </summary>
        public double MaxCpuPercentage { get; set; } = 80.0;

        /// <summary>
        /// Gets or sets the maximum memory to use in bytes.
        /// </summary>
        public long MaxMemoryBytes { get; set; } = 1024 * 1024 * 1024; // 1GB

        /// <summary>
        /// Gets or sets the maximum number of concurrent requests.
        /// </summary>
        public int MaxConcurrentRequests { get; set; } = 10;

        /// <summary>
        /// Gets or sets the priority level for resource allocation.
        /// </summary>
        public PriorityLevel PriorityLevel { get; set; } = PriorityLevel.Normal;
    }

    /// <summary>
    /// Priority levels for processing and resource allocation.
    /// </summary>
    public enum PriorityLevel
    {
        Low,
        Normal,
        High,
        Critical
    }

    /// <summary>
    /// Processing optimization recommendations.
    /// </summary>
    public class ProcessingOptimization
    {
        /// <summary>
        /// Gets or sets the recommended processing strategy.
        /// </summary>
        public ProcessingStrategy RecommendedStrategy { get; set; } = new ProcessingStrategy();

        /// <summary>
        /// Gets or sets the optimization recommendations.
        /// </summary>
        public List<string> Recommendations { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the expected improvement percentage.
        /// </summary>
        public double ExpectedImprovement { get; set; }

        /// <summary>
        /// Gets or sets the identified bottlenecks.
        /// </summary>
        public List<string> Bottlenecks { get; set; } = new List<string>();
    }

    /// <summary>
    /// Processing performance metrics.
    /// </summary>
    public class ProcessingPerformance
    {
        /// <summary>
        /// Gets or sets the total number of requests processed.
        /// </summary>
        public int TotalRequestsProcessed { get; set; }

        /// <summary>
        /// Gets or sets the average processing time in milliseconds.
        /// </summary>
        public double AverageProcessingTime { get; set; }

        /// <summary>
        /// Gets or sets the average response size in characters.
        /// </summary>
        public double AverageResponseSize { get; set; }

        /// <summary>
        /// Gets or sets the success rate (0.0 to 1.0).
        /// </summary>
        public double SuccessRate { get; set; }

        /// <summary>
        /// Gets or sets the resource utilization percentage.
        /// </summary>
        public double ResourceUtilization { get; set; }

        /// <summary>
        /// Gets or sets the processing trends.
        /// </summary>
        public List<ProcessingTrend> ProcessingTrends { get; set; } = new List<ProcessingTrend>();
    }

    /// <summary>
    /// Processing metrics for optimization.
    /// </summary>
    public class ProcessingMetrics
    {
        /// <summary>
        /// Gets or sets the request type.
        /// </summary>
        public string RequestType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the processing time.
        /// </summary>
        public TimeSpan ProcessingTime { get; set; }

        /// <summary>
        /// Gets or sets the response size in characters.
        /// </summary>
        public int ResponseSize { get; set; }

        /// <summary>
        /// Gets or sets whether the processing was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the resource utilization percentage.
        /// </summary>
        public double ResourceUtilization { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the processing.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Processing trend analysis.
    /// </summary>
    public class ProcessingTrend
    {
        /// <summary>
        /// Gets or sets the metric being tracked.
        /// </summary>
        public string Metric { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the trend direction.
        /// </summary>
        public TrendDirection Trend { get; set; }

        /// <summary>
        /// Gets or sets the change percentage.
        /// </summary>
        public double ChangePercentage { get; set; }
    }

    /// <summary>
    /// Direction of a processing trend.
    /// </summary>
    public enum TrendDirection
    {
        Improving,
        Degrading,
        Stable
    }
} 