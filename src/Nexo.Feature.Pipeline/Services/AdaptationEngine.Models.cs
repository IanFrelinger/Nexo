using System;
using System.Collections.Generic;
using Nexo.Feature.Pipeline.Interfaces;

namespace Nexo.Feature.Pipeline.Services
{
    /// <summary>
    /// Data models for AdaptationEngine.
    /// </summary>
    public partial class AdaptationEngine
    {
        // This partial class contains the data models used by AdaptationEngine
    }

    /// <summary>
    /// Strategy update information.
    /// </summary>
    public class StrategyUpdate
    {
        /// <summary>
        /// Gets or sets the strategy name.
        /// </summary>
        public string StrategyName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the update type.
        /// </summary>
        public string UpdateType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the new value.
        /// </summary>
        public object NewValue { get; set; } = new object();
    }

    /// <summary>
    /// System optimization information.
    /// </summary>
    public class SystemOptimization
    {
        /// <summary>
        /// Gets or sets the optimization type.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optimization description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optimization priority.
        /// </summary>
        public RecommendationPriority Priority { get; set; } = RecommendationPriority.Medium;
    }

    /// <summary>
    /// Resource adjustment information.
    /// </summary>
    public class ResourceAdjustment
    {
        /// <summary>
        /// Gets or sets the resource type.
        /// </summary>
        public string ResourceType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the adjustment type.
        /// </summary>
        public string AdjustmentType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the adjustment description.
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }
}
