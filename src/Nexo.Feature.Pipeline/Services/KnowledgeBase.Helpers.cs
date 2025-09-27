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
    /// Helper methods for KnowledgeBase
    /// </summary>
    public partial class KnowledgeBase
    {
        /// <summary>
        /// Extracts insights from execution patterns.
        /// </summary>
        private async Task<List<LearningInsight>> ExtractInsightsFromPatternsAsync(
            Dictionary<string, object> patterns,
            CancellationToken cancellationToken)
        {
            var insights = new List<LearningInsight>();

            // Extract execution time insights
            if (patterns.ContainsKey("executionTime") && patterns["executionTime"] is long executionTime)
            {
                insights.Add(new LearningInsight
                {
                    Type = "ExecutionTime",
                    Description = $"Execution time: {executionTime}ms",
                    ConfidenceLevel = 95.0,
                    Data = new Dictionary<string, object> { { "executionTime", executionTime } },
                    Source = "ExecutionPattern"
                });
            }

            // Extract success rate insights
            if (patterns.ContainsKey("success") && patterns["success"] is bool success)
            {
                insights.Add(new LearningInsight
                {
                    Type = "SuccessRate",
                    Description = $"Execution success: {success}",
                    ConfidenceLevel = 100.0,
                    Data = new Dictionary<string, object> { { "success", success } },
                    Source = "ExecutionPattern"
                });
            }

            // Extract error count insights
            if (patterns.ContainsKey("errorCount") && patterns["errorCount"] is int errorCount)
            {
                insights.Add(new LearningInsight
                {
                    Type = "ErrorCount",
                    Description = $"Error count: {errorCount}",
                    ConfidenceLevel = 90.0,
                    Data = new Dictionary<string, object> { { "errorCount", errorCount } },
                    Source = "ExecutionPattern"
                });
            }

            return await Task.FromResult(insights);
        }

        /// <summary>
        /// Stores a learning insight.
        /// </summary>
        private async Task StoreInsightAsync(
            LearningInsight insight,
            CancellationToken cancellationToken)
        {
            if (!_insights.ContainsKey(insight.Type))
            {
                _insights[insight.Type] = new List<LearningInsight>();
            }

            _insights[insight.Type].Add(insight);

            // Keep only the most recent insights (limit to 100 per type)
            if (_insights[insight.Type].Count > 100)
            {
                _insights[insight.Type] = _insights[insight.Type]
                    .OrderByDescending(i => i.CreatedAt)
                    .Take(100)
                    .ToList();
            }

            await Task.CompletedTask;
        }
    }
}
