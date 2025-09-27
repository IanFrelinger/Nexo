using System;

namespace Nexo.Feature.Pipeline.Interfaces.Optimization
{
    /// <summary>
    /// Resource optimization recommendation.
    /// </summary>
    public partial class ResourceOptimizationRecommendation
    {
        /// <summary>
        /// Gets or sets the recommendation identifier.
        /// </summary>
        public string RecommendationId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the optimization type.
        /// </summary>
        public OptimizationType Type { get; set; }

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
        /// Gets or sets the target resource type.
        /// </summary>
        public ResourceType TargetResourceType { get; set; }
    }
}
