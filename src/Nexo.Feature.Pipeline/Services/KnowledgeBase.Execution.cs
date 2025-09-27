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
    /// Execution-related functionality
    /// </summary>
    public partial class KnowledgeBase
    {
        /// <summary>
        /// Updates the knowledge base with execution results and patterns.
        /// </summary>
        public async Task UpdateWithExecutionResultAsync(
            PipelineExecutionResult result,
            Dictionary<string, object> patterns,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating knowledge base with execution result {ExecutionId}", result.ExecutionId);

            try
            {
                // Store execution pattern
                var executionPattern = new ExecutionPattern
                {
                    ExecutionId = result.ExecutionId,
                    Patterns = patterns,
                    Success = result.Success,
                    ExecutionTimeMs = result.ExecutionTimeMs,
                    Timestamp = DateTime.UtcNow
                };

                _executionPatterns.Add(executionPattern);

                // Extract insights from patterns
                var insights = await ExtractInsightsFromPatternsAsync(patterns, cancellationToken);
                foreach (var insight in insights)
                {
                    await StoreInsightAsync(insight, cancellationToken);
                }

                _logger.LogInformation("Updated knowledge base with {InsightCount} insights", insights.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating knowledge base with execution result {ExecutionId}", result.ExecutionId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves historical performance data for similar configurations.
        /// </summary>
        public async Task<Dictionary<string, object>> GetHistoricalPerformanceAsync(
            PipelineConfiguration configuration,
            ExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Retrieving historical performance data for configuration {ConfigurationId}", 
                configuration.Id);

            try
            {
                // Find similar execution patterns
                var similarPatterns = _executionPatterns
                    .Where(p => p.Success)
                    .OrderByDescending(p => p.Timestamp)
                    .Take(10)
                    .ToList();

                var historicalData = new Dictionary<string, object>
                {
                    { "similarExecutions", similarPatterns.Count },
                    { "averageExecutionTime", similarPatterns.Any() ? 
                        similarPatterns.Average(p => p.ExecutionTimeMs) : 0 },
                    { "successRate", similarPatterns.Any() ? 
                        (double)similarPatterns.Count(p => p.Success) / similarPatterns.Count * 100 : 0 },
                    { "recentPatterns", similarPatterns.Select(p => new { 
                        p.ExecutionId, p.ExecutionTimeMs, p.Success, p.Timestamp }) }
                };

                _logger.LogDebug("Retrieved historical performance data: {ExecutionCount} similar executions", 
                    similarPatterns.Count);

                return await Task.FromResult(historicalData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving historical performance data");
                return new Dictionary<string, object>();
            }
        }
    }
}
