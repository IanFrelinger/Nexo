using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces.Performance
{
    /// <summary>
    /// Interface for model loading optimization service.
    /// Part of Phase 3.3 performance optimization features.
    /// </summary>
    public interface IModelLoadingOptimizationService
    {
        /// <summary>
        /// Optimizes model loading based on usage patterns and resource availability.
        /// </summary>
        /// <param name="modelName">Name of the model to optimize.</param>
        /// <param name="context">Loading context and constraints.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Model loading optimization recommendations.</returns>
        Task<ModelLoadingOptimization> OptimizeModelLoadingAsync(
            string modelName, 
            ModelLoadingContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Preloads models based on usage patterns and predictions.
        /// </summary>
        /// <param name="modelNames">Names of models to preload.</param>
        /// <param name="context">Preloading context and constraints.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Preloading results and statistics.</returns>
        Task<PreloadingResult> PreloadModelsAsync(
            IEnumerable<string> modelNames,
            PreloadingContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Monitors model performance and adjusts loading strategies.
        /// </summary>
        /// <param name="modelName">Name of the model to monitor.</param>
        /// <param name="monitoringDuration">Duration to monitor performance.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Model performance report.</returns>
        Task<ModelPerformanceReport> MonitorModelPerformanceAsync(
            string modelName,
            TimeSpan monitoringDuration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets optimization recommendations for model loading.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of optimization recommendations.</returns>
        Task<List<ModelOptimizationRecommendation>> GetOptimizationRecommendationsAsync(
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Model loading context.
    /// </summary>
    public class ModelLoadingContext
    {
        public LoadingPriority Priority { get; set; } = LoadingPriority.Normal;
        public int ExpectedUsage { get; set; }
        public ResourceConstraints ResourceConstraints { get; set; } = new ResourceConstraints();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Preloading context.
    /// </summary>
    public class PreloadingContext
    {
        public int MaxConcurrentPreloads { get; set; } = 5;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(10);
        public ResourceConstraints ResourceConstraints { get; set; } = new ResourceConstraints();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Resource constraints.
    /// </summary>
    public class ResourceConstraints
    {
        public int MemoryLimit { get; set; } = 2048; // MB
        public int CpuLimit { get; set; } = 80; // Percentage
        public int NetworkLimit { get; set; } = 100; // Mbps
        public int StorageLimit { get; set; } = 5000; // MB
    }

    /// <summary>
    /// Model loading optimization result.
    /// </summary>
    public class ModelLoadingOptimization
    {
        public string ModelName { get; set; } = string.Empty;
        public DateTimeOffset OptimizedAt { get; set; }
        public ModelLoadingStrategy LoadingStrategy { get; set; }
        public List<PreloadingRecommendation> PreloadingRecommendations { get; set; } = new List<PreloadingRecommendation>();
        public ResourceAllocation ResourceAllocation { get; set; } = new ResourceAllocation();
        public PerformancePrediction PerformancePredictions { get; set; } = new PerformancePrediction();
    }

    /// <summary>
    /// Preloading result.
    /// </summary>
    public class PreloadingResult
    {
        public int TotalModels { get; set; }
        public int SuccessfullyPreloaded { get; set; }
        public int FailedPreloads { get; set; }
        public List<ModelPreloadingResult> Results { get; set; } = new List<ModelPreloadingResult>();
        public TimeSpan TotalPreloadingTime { get; set; }
        public TimeSpan AveragePreloadingTime { get; set; }
    }

    /// <summary>
    /// Model preloading result.
    /// </summary>
    public class ModelPreloadingResult
    {
        public string ModelName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public TimeSpan PreloadingTime { get; set; }
        public double MemoryUsed { get; set; }
        public double CpuUsed { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Model performance report.
    /// </summary>
    public class ModelPerformanceReport
    {
        public string ModelName { get; set; } = string.Empty;
        public DateTimeOffset MonitoringStartTime { get; set; }
        public DateTimeOffset MonitoringEndTime { get; set; }
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public TimeSpan PeakResponseTime { get; set; }
        public double AverageMemoryUsage { get; set; }
        public double PeakMemoryUsage { get; set; }
        public double CpuUtilization { get; set; }
        public double Throughput { get; set; }
        public double ErrorRate { get; set; }
    }

    /// <summary>
    /// Model optimization recommendation.
    /// </summary>
    public class ModelOptimizationRecommendation
    {
        public OptimizationType Type { get; set; }
        public RecommendationPriority Priority { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PotentialImprovement { get; set; } = string.Empty;
        public ImplementationEffort Effort { get; set; }
    }

    /// <summary>
    /// Preloading recommendation.
    /// </summary>
    public class PreloadingRecommendation
    {
        public string ModelName { get; set; } = string.Empty;
        public PreloadingPriority Priority { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string EstimatedBenefit { get; set; } = string.Empty;
        public ResourceCost? ResourceCost { get; set; }
    }

    /// <summary>
    /// Resource allocation.
    /// </summary>
    public class ResourceAllocation
    {
        public int MemoryAllocation { get; set; }
        public int CpuAllocation { get; set; }
        public int NetworkAllocation { get; set; }
        public int StorageAllocation { get; set; }
    }

    /// <summary>
    /// Performance prediction.
    /// </summary>
    public class PerformancePrediction
    {
        public TimeSpan EstimatedLoadingTime { get; set; }
        public double EstimatedMemoryUsage { get; set; }
        public double EstimatedCpuUsage { get; set; }
        public double Confidence { get; set; }
        public List<string> RiskFactors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Resource cost.
    /// </summary>
    public class ResourceCost
    {
        public double MemoryCost { get; set; }
        public double CpuCost { get; set; }
        public double NetworkCost { get; set; }
        public double StorageCost { get; set; }
    }

    /// <summary>
    /// Loading priority levels.
    /// </summary>
    public enum LoadingPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    /// <summary>
    /// Model loading strategies.
    /// </summary>
    public enum ModelLoadingStrategy
    {
        OnDemand,
        Preload,
        LazyLoad,
        Cache
    }

    /// <summary>
    /// Preloading priority levels.
    /// </summary>
    public enum PreloadingPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Optimization types.
    /// </summary>
    public enum OptimizationType
    {
        Preloading,
        Compression,
        Caching,
        ResourceAllocation,
        PerformanceTuning
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

    /// <summary>
    /// Implementation effort levels.
    /// </summary>
    public enum ImplementationEffort
    {
        Low,
        Medium,
        High,
        VeryHigh
    }
}