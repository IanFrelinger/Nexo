using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Pipeline.Models;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Feature.Pipeline.Interfaces
{
    /// <summary>
    /// Interface for real-time adaptation services that enable continuous learning and optimization.
    /// </summary>
    public interface IRealTimeAdaptationService
    {
        /// <summary>
        /// Learns from pipeline execution results to improve future performance.
        /// </summary>
        /// <param name="result">The pipeline execution result to learn from.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Task representing the learning operation.</returns>
        Task LearnFromExecutionAsync(
            PipelineExecutionResult result,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adapts the system to the current environment context.
        /// </summary>
        /// <param name="context">The environment context to adapt to.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Task representing the adaptation operation.</returns>
        Task AdaptToEnvironmentAsync(
            EnvironmentContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets adaptation recommendations based on current system state.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>List of adaptation recommendations.</returns>
        Task<List<AdaptationRecommendation>> GetRecommendationsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes user feedback to improve system behavior.
        /// </summary>
        /// <param name="feedback">The user feedback to process.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Task representing the feedback processing operation.</returns>
        Task ProcessUserFeedbackAsync(
            UserFeedback feedback,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current adaptation state of the system.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Current adaptation state information.</returns>
        Task<AdaptationState> GetAdaptationStateAsync(
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Environment context for adaptation.
    /// </summary>
    public class EnvironmentContext
    {
        /// <summary>
        /// Gets or sets the environment type.
        /// </summary>
        public EnvironmentType EnvironmentType { get; set; } = EnvironmentType.Development;

        /// <summary>
        /// Gets or sets the environment name.
        /// </summary>
        public string EnvironmentName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the environment properties.
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the performance requirements for this environment.
        /// </summary>
        public PerformanceRequirements PerformanceRequirements { get; set; } = new PerformanceRequirements();

        /// <summary>
        /// Gets or sets the resource constraints for this environment.
        /// </summary>
        public ResourceConstraints ResourceConstraints { get; set; } = new ResourceConstraints();

        /// <summary>
        /// Gets or sets the timestamp when this context was created.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Environment types for adaptation.
    /// </summary>
    public enum EnvironmentType
    {
        /// <summary>
        /// Development environment.
        /// </summary>
        Development,

        /// <summary>
        /// Testing environment.
        /// </summary>
        Testing,

        /// <summary>
        /// Staging environment.
        /// </summary>
        Staging,

        /// <summary>
        /// Production environment.
        /// </summary>
        Production,

        /// <summary>
        /// Custom environment.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Resource constraints for environment adaptation.
    /// </summary>
    public class ResourceConstraints
    {
        /// <summary>
        /// Gets or sets the maximum CPU usage percentage.
        /// </summary>
        public double MaxCpuUsagePercentage { get; set; } = 80.0;

        /// <summary>
        /// Gets or sets the maximum memory usage in MB.
        /// </summary>
        public long MaxMemoryUsageMB { get; set; } = 1024;

        /// <summary>
        /// Gets or sets the maximum execution time in milliseconds.
        /// </summary>
        public long MaxExecutionTimeMs { get; set; } = 30000;

        /// <summary>
        /// Gets or sets the maximum concurrent operations.
        /// </summary>
        public int MaxConcurrentOperations { get; set; } = 10;

        /// <summary>
        /// Gets or sets whether aggressive optimization is allowed.
        /// </summary>
        public bool AllowAggressiveOptimization { get; set; } = false;

        /// <summary>
        /// Gets or sets whether experimental features are enabled.
        /// </summary>
        public bool EnableExperimentalFeatures { get; set; } = false;
    }

    /// <summary>
    /// Adaptation recommendation for system improvement.
    /// </summary>
    public class AdaptationRecommendation
    {
        /// <summary>
        /// Gets or sets the recommendation identifier.
        /// </summary>
        public string RecommendationId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the recommendation type.
        /// </summary>
        public AdaptationType Type { get; set; }

        /// <summary>
        /// Gets or sets the recommendation title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the recommendation description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the expected improvement percentage.
        /// </summary>
        public double ExpectedImprovementPercentage { get; set; }

        /// <summary>
        /// Gets or sets the implementation complexity.
        /// </summary>
        public ImplementationComplexity ImplementationComplexity { get; set; }

        /// <summary>
        /// Gets or sets the confidence level (0-100).
        /// </summary>
        public double ConfidenceLevel { get; set; }

        /// <summary>
        /// Gets or sets the recommendation priority.
        /// </summary>
        public RecommendationPriority Priority { get; set; } = RecommendationPriority.Medium;

        /// <summary>
        /// Gets or sets the recommendation details.
        /// </summary>
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the timestamp when this recommendation was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Types of adaptation recommendations.
    /// </summary>
    public enum AdaptationType
    {
        /// <summary>
        /// Performance optimization recommendation.
        /// </summary>
        PerformanceOptimization,

        /// <summary>
        /// Resource allocation recommendation.
        /// </summary>
        ResourceAllocation,

        /// <summary>
        /// Configuration optimization recommendation.
        /// </summary>
        ConfigurationOptimization,

        /// <summary>
        /// Strategy adaptation recommendation.
        /// </summary>
        StrategyAdaptation,

        /// <summary>
        /// Environment-specific recommendation.
        /// </summary>
        EnvironmentAdaptation,

        /// <summary>
        /// User experience improvement recommendation.
        /// </summary>
        UserExperienceImprovement
    }

    /// <summary>
    /// Recommendation priority levels.
    /// </summary>
    public enum RecommendationPriority
    {
        /// <summary>
        /// Low priority recommendation.
        /// </summary>
        Low,

        /// <summary>
        /// Medium priority recommendation.
        /// </summary>
        Medium,

        /// <summary>
        /// High priority recommendation.
        /// </summary>
        High,

        /// <summary>
        /// Critical priority recommendation.
        /// </summary>
        Critical
    }

    /// <summary>
    /// User feedback for system improvement.
    /// </summary>
    public class UserFeedback
    {
        /// <summary>
        /// Gets or sets the feedback identifier.
        /// </summary>
        public string FeedbackId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the feedback type.
        /// </summary>
        public FeedbackType Type { get; set; }

        /// <summary>
        /// Gets or sets the feedback rating (1-5).
        /// </summary>
        public int Rating { get; set; }

        /// <summary>
        /// Gets or sets the feedback message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the context of the feedback.
        /// </summary>
        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the timestamp when this feedback was provided.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the execution ID this feedback relates to.
        /// </summary>
        public string? ExecutionId { get; set; }
    }

    /// <summary>
    /// Types of user feedback.
    /// </summary>
    public enum FeedbackType
    {
        /// <summary>
        /// Performance feedback.
        /// </summary>
        Performance,

        /// <summary>
        /// Usability feedback.
        /// </summary>
        Usability,

        /// <summary>
        /// Feature request feedback.
        /// </summary>
        FeatureRequest,

        /// <summary>
        /// Bug report feedback.
        /// </summary>
        BugReport,

        /// <summary>
        /// General feedback.
        /// </summary>
        General
    }

    /// <summary>
    /// Current adaptation state of the system.
    /// </summary>
    public class AdaptationState
    {
        /// <summary>
        /// Gets or sets the state identifier.
        /// </summary>
        public string StateId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the current environment type.
        /// </summary>
        public EnvironmentType CurrentEnvironment { get; set; }

        /// <summary>
        /// Gets or sets the adaptation level (0-100).
        /// </summary>
        public double AdaptationLevel { get; set; }

        /// <summary>
        /// Gets or sets the learning progress percentage.
        /// </summary>
        public double LearningProgress { get; set; }

        /// <summary>
        /// Gets or sets the number of adaptations performed.
        /// </summary>
        public int AdaptationsPerformed { get; set; }

        /// <summary>
        /// Gets or sets the last adaptation timestamp.
        /// </summary>
        public DateTime LastAdaptationTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the current performance metrics.
        /// </summary>
        public Dictionary<string, object> PerformanceMetrics { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the active recommendations count.
        /// </summary>
        public int ActiveRecommendationsCount { get; set; }

        /// <summary>
        /// Gets or sets the system health status.
        /// </summary>
        public SystemHealthStatus HealthStatus { get; set; } = SystemHealthStatus.Healthy;
    }

    /// <summary>
    /// System health status indicators.
    /// </summary>
    public enum SystemHealthStatus
    {
        /// <summary>
        /// System is healthy.
        /// </summary>
        Healthy,

        /// <summary>
        /// System has minor issues.
        /// </summary>
        Warning,

        /// <summary>
        /// System has significant issues.
        /// </summary>
        Critical,

        /// <summary>
        /// System is in maintenance mode.
        /// </summary>
        Maintenance
    }

    /// <summary>
    /// Adaptation action to be applied to the system.
    /// </summary>
    public class AdaptationAction
    {
        /// <summary>
        /// Gets or sets the adaptation type.
        /// </summary>
        public AdaptationType Type { get; set; }

        /// <summary>
        /// Gets or sets the adaptation description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the adaptation priority.
        /// </summary>
        public RecommendationPriority Priority { get; set; } = RecommendationPriority.Medium;

        /// <summary>
        /// Gets or sets the adaptation parameters.
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }
}
