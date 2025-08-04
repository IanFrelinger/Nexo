using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Shared.Interfaces.Resource
{
    /// <summary>
    /// Interface for resource optimization and adaptive performance tuning.
    /// </summary>
    public interface IResourceOptimizer
    {
        /// <summary>
        /// Performs adaptive performance tuning based on current resource usage.
        /// </summary>
        Task<OptimizationResult> OptimizeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs resource-aware pipeline scheduling and throttling.
        /// </summary>
        Task<ThrottlingResult> CalculateThrottlingAsync(
            PipelineExecutionRequest request, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a custom optimization rule.
        /// </summary>
        void AddOptimizationRule(string ruleId, OptimizationRule rule);

        /// <summary>
        /// Removes an optimization rule.
        /// </summary>
        void RemoveOptimizationRule(string ruleId);

        /// <summary>
        /// Gets optimization history for analysis.
        /// </summary>
        IEnumerable<OptimizationHistory> GetOptimizationHistory();
    }

    /// <summary>
    /// Represents an optimization rule.
    /// </summary>
    public class OptimizationRule
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Func<ResourceUsage, Task<bool>> Condition { get; set; } = _ => Task.FromResult(false);
        public Func<ResourceUsage, Task<OptimizationRecommendation>> Action { get; set; } = _ => Task.FromResult(new OptimizationRecommendation());
    }

    /// <summary>
    /// Represents an optimization recommendation.
    /// </summary>
    public class OptimizationRecommendation
    {
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
        public int Priority { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Represents an optimization result.
    /// </summary>
    public class OptimizationResult
    {
        public DateTime Timestamp { get; set; }
        public List<OptimizationRecommendation> Recommendations { get; set; } = new List<OptimizationRecommendation>();
    }

    /// <summary>
    /// Represents optimization history.
    /// </summary>
    public class OptimizationHistory
    {
        public DateTime Timestamp { get; set; }
        public ResourceUsage ResourceUsage { get; set; } = new ResourceUsage();
        public List<OptimizationRecommendation> Recommendations { get; set; } = new List<OptimizationRecommendation>();
    }

    /// <summary>
    /// Represents a pipeline execution request.
    /// </summary>
    public class PipelineExecutionRequest
    {
        public string PipelineId { get; set; } = string.Empty;
        public int EstimatedCpuUsage { get; set; }
        public long EstimatedMemoryUsage { get; set; }
        public TimeSpan EstimatedDuration { get; set; }
        public int Priority { get; set; }
    }

    /// <summary>
    /// Represents a throttling result.
    /// </summary>
    public class ThrottlingResult
    {
        public bool ShouldThrottle { get; set; }
        public ThrottlingLevel ThrottlingLevel { get; set; }
        public TimeSpan RecommendedDelay { get; set; }
    }

    /// <summary>
    /// Represents throttling levels.
    /// </summary>
    public enum ThrottlingLevel
    {
        None,
        Low,
        Medium,
        High
    }
} 