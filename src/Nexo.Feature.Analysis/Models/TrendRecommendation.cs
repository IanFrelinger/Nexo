using System.Collections.Generic;
using Nexo.Feature.Analysis.Enums;

namespace Nexo.Feature.Analysis.Models
{
    /// <summary>
    /// Recommendation based on trend analysis.
    /// </summary>
    public class TrendRecommendation
    {
        /// <summary>
        /// Type of recommendation.
        /// </summary>
        public RecommendationType Type { get; set; }

        /// <summary>
        /// Title of the recommendation.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Description of the recommendation.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Priority of the recommendation.
        /// </summary>
        public RecommendationPriority Priority { get; set; }

        /// <summary>
        /// Estimated impact of implementing the recommendation.
        /// </summary>
        public string EstimatedImpact { get; set; } = string.Empty;

        /// <summary>
        /// Suggested actions to implement the recommendation.
        /// </summary>
        public List<string> SuggestedActions { get; set; } = new List<string>();
    }
} 