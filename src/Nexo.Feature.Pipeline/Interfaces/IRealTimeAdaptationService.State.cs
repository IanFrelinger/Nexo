using System;
using System.Collections.Generic;

namespace Nexo.Feature.Pipeline.Interfaces
{
    /// <summary>
    /// Adaptation state and related models
    /// </summary>
    public partial interface IRealTimeAdaptationService
    {
        // State models are defined in separate files
    }

    /// <summary>
    /// Current adaptation state of the system.
    /// </summary>
    public partial class AdaptationState
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
    public partial class AdaptationAction
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
