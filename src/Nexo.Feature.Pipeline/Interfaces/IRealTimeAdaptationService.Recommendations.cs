using System;
using System.Collections.Generic;

namespace Nexo.Feature.Pipeline.Interfaces
{
    /// <summary>
    /// Adaptation recommendations and related models
    /// </summary>
    public partial interface IRealTimeAdaptationService
    {
        // Recommendation models are defined in separate files
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
}
