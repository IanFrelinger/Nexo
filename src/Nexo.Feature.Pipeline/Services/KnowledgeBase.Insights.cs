using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Feature.Pipeline.Models;
using ExecutionContext = Nexo.Feature.Pipeline.Models.ExecutionContext;

namespace Nexo.Feature.Pipeline.Services
{
    /// <summary>
    /// Insights retrieval functionality
    /// </summary>
    public partial class KnowledgeBase
    {
        /// <summary>
        /// Retrieves learning insights based on patterns.
        /// </summary>
        public async Task<List<LearningInsight>> GetLearningInsightsAsync(
            string patternType,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Retrieving learning insights for pattern type: {PatternType}", patternType);

            try
            {
                if (_insights.ContainsKey(patternType))
                {
                    var insights = _insights[patternType]
                        .OrderByDescending(i => i.ConfidenceLevel)
                        .ToList();

                    _logger.LogDebug("Retrieved {InsightCount} insights for pattern type {PatternType}", 
                        insights.Count, patternType);

                    return await Task.FromResult(insights);
                }

                return new List<LearningInsight>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving learning insights for pattern type {PatternType}", patternType);
                return new List<LearningInsight>();
            }
        }
    }
}
