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
    /// Knowledge base implementation that stores and retrieves learning insights.
    /// </summary>
    public class KnowledgeBase : IKnowledgeBase
    {
        private readonly ILogger<KnowledgeBase> _logger;
        private readonly Dictionary<string, List<LearningInsight>> _insights;
        private readonly Dictionary<string, Dictionary<string, object>> _userPreferences;
        private readonly List<ExecutionPattern> _executionPatterns;

        public KnowledgeBase(ILogger<KnowledgeBase> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _insights = new Dictionary<string, List<LearningInsight>>();
            _userPreferences = new Dictionary<string, Dictionary<string, object>>();
            _executionPatterns = new List<ExecutionPattern>();
        }

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
        /// Stores a bottleneck pattern for future analysis.
        /// </summary>
        public async Task StoreBottleneckPatternAsync(
            PerformanceBottleneck bottleneck,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Storing bottleneck pattern: {Type} - {Description}", 
                bottleneck.Type, bottleneck.Description);

            try
            {
                var insight = new LearningInsight
                {
                    Type = "Bottleneck",
                    Description = $"Performance bottleneck: {bottleneck.Description}",
                    ConfidenceLevel = 90.0,
                    Data = new Dictionary<string, object>
                    {
                        { "bottleneckType", bottleneck.Type.ToString() },
                        { "severity", bottleneck.Severity.ToString() },
                        { "impactPercentage", bottleneck.ImpactPercentage },
                        { "affectedComponent", bottleneck.AffectedComponent }
                    },
                    Source = "PerformanceAnalysis"
                };

                await StoreInsightAsync(insight, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing bottleneck pattern");
                throw;
            }
        }

        /// <summary>
        /// Stores a strength pattern for future analysis.
        /// </summary>
        public async Task StoreStrengthPatternAsync(
            PerformanceStrength strength,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Storing strength pattern: {Type} - {Description}", 
                strength.Type, strength.Description);

            try
            {
                var insight = new LearningInsight
                {
                    Type = "Strength",
                    Description = $"Performance strength: {strength.Description}",
                    ConfidenceLevel = 85.0,
                    Data = new Dictionary<string, object>
                    {
                        { "strengthType", strength.Type.ToString() },
                        { "benefitPercentage", strength.BenefitPercentage },
                        { "sourceComponent", strength.SourceComponent }
                    },
                    Source = "PerformanceAnalysis"
                };

                await StoreInsightAsync(insight, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing strength pattern");
                throw;
            }
        }

        /// <summary>
        /// Stores an optimization opportunity for future analysis.
        /// </summary>
        public async Task StoreOptimizationOpportunityAsync(
            OptimizationOpportunity opportunity,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Storing optimization opportunity: {Type} - {Description}", 
                opportunity.Type, opportunity.Description);

            try
            {
                var insight = new LearningInsight
                {
                    Type = "OptimizationOpportunity",
                    Description = $"Optimization opportunity: {opportunity.Description}",
                    ConfidenceLevel = 80.0,
                    Data = new Dictionary<string, object>
                    {
                        { "opportunityType", opportunity.Type.ToString() },
                        { "potentialImprovement", opportunity.PotentialImprovementPercentage },
                        { "implementationComplexity", opportunity.ImplementationComplexity.ToString() },
                        { "targetComponent", opportunity.TargetComponent }
                    },
                    Source = "PerformanceAnalysis"
                };

                await StoreInsightAsync(insight, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing optimization opportunity");
                throw;
            }
        }

        /// <summary>
        /// Stores user feedback for future analysis.
        /// </summary>
        public async Task StoreUserFeedbackAsync(
            UserFeedback feedback,
            Dictionary<string, object> analysis,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Storing user feedback {FeedbackId} from user {UserId}", 
                feedback.FeedbackId, feedback.UserId);

            try
            {
                var insight = new LearningInsight
                {
                    Type = "UserFeedback",
                    Description = $"User feedback: {feedback.Message}",
                    ConfidenceLevel = 75.0,
                    Data = new Dictionary<string, object>
                    {
                        { "feedbackType", feedback.Type.ToString() },
                        { "rating", feedback.Rating },
                        { "userId", feedback.UserId },
                        { "analysis", analysis }
                    },
                    Source = "UserFeedback"
                };

                await StoreInsightAsync(insight, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing user feedback");
                throw;
            }
        }

        /// <summary>
        /// Updates user preferences based on feedback analysis.
        /// </summary>
        public async Task UpdateUserPreferencesAsync(
            string userId,
            Dictionary<string, object> feedbackAnalysis,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating user preferences for user {UserId}", userId);

            try
            {
                if (!_userPreferences.ContainsKey(userId))
                {
                    _userPreferences[userId] = new Dictionary<string, object>();
                }

                // Update preferences based on feedback analysis
                if (feedbackAnalysis.ContainsKey("rating"))
                {
                    _userPreferences[userId]["lastRating"] = feedbackAnalysis["rating"];
                }

                if (feedbackAnalysis.ContainsKey("sentiment"))
                {
                    _userPreferences[userId]["preferredSentiment"] = feedbackAnalysis["sentiment"];
                }

                _userPreferences[userId]["lastUpdated"] = DateTime.UtcNow;

                _logger.LogInformation("Updated user preferences for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user preferences for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Updates the adaptation state with new context and adaptations.
        /// </summary>
        public async Task UpdateAdaptationStateAsync(
            EnvironmentContext context,
            List<AdaptationAction> adaptations,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating adaptation state for environment {EnvironmentType}", 
                context.EnvironmentType);

            try
            {
                var insight = new LearningInsight
                {
                    Type = "AdaptationState",
                    Description = $"Environment adaptation: {context.EnvironmentType}",
                    ConfidenceLevel = 90.0,
                    Data = new Dictionary<string, object>
                    {
                        { "environmentType", context.EnvironmentType.ToString() },
                        { "environmentName", context.EnvironmentName },
                        { "adaptationCount", adaptations.Count },
                        { "adaptations", adaptations.Select(a => new { a.Type, a.Description, a.Priority }) }
                    },
                    Source = "AdaptationEngine"
                };

                await StoreInsightAsync(insight, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating adaptation state");
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

        /// <summary>
        /// Retrieves user preferences for a specific user.
        /// </summary>
        public async Task<Dictionary<string, object>> GetUserPreferencesAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Retrieving user preferences for user {UserId}", userId);

            try
            {
                if (_userPreferences.ContainsKey(userId))
                {
                    return await Task.FromResult(_userPreferences[userId]);
                }

                return new Dictionary<string, object>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user preferences for user {UserId}", userId);
                return new Dictionary<string, object>();
            }
        }

        #region Private Helper Methods

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

        #endregion
    }

    /// <summary>
    /// Execution pattern for learning and analysis.
    /// </summary>
    public class ExecutionPattern
    {
        /// <summary>
        /// Gets or sets the execution identifier.
        /// </summary>
        public string ExecutionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the execution patterns.
        /// </summary>
        public Dictionary<string, object> Patterns { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets whether the execution was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the execution time in milliseconds.
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this pattern was recorded.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
