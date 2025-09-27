using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Learning;
using Nexo.Core.Application.Models.Learning;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;

namespace Nexo.Infrastructure.Services.Learning
{
    /// <summary>
    /// Metrics functionality for OptimizationRecommendationService.
    /// Handles optimization metrics and statistics generation.
    /// </summary>
    public partial class OptimizationRecommendationService
    {
        /// <summary>
        /// Gets optimization metrics and statistics.
        /// </summary>
        public async Task<OptimizationMetrics> GetOptimizationMetricsAsync(
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting optimization metrics and statistics");

            try
            {
                // Use AI to generate optimization metrics
                var prompt = @"
Generate optimization metrics and statistics:
- Total recommendations count
- Applied recommendations count
- Pending recommendations count
- Average impact score
- Average effort score
- Success rate
- Category breakdown
- Performance metrics

Requirements:
- Calculate comprehensive metrics
- Generate category breakdowns
- Provide performance indicators
- Create statistical summaries
- Generate insights

Generate comprehensive optimization metrics.
";

                var request = new ModelRequest { Input = prompt };
                var response = await _modelOrchestrator.ProcessAsync(request, cancellationToken);
                
                var metrics = new OptimizationMetrics
                {
                    TotalRecommendations = ParseTotalRecommendations(response.Response),
                    AppliedRecommendations = ParseAppliedRecommendationsCount(response.Response),
                    PendingRecommendations = ParsePendingRecommendations(response.Response),
                    AverageImpact = ParseAverageImpact(response.Response),
                    AverageEffort = ParseAverageEffort(response.Response),
                    SuccessRate = ParseSuccessRate(response.Response),
                    CategoryMetrics = ParseCategoryMetrics(response.Response),
                    PerformanceMetrics = ParsePerformanceMetrics(response.Response),
                    GeneratedAt = DateTimeOffset.UtcNow.DateTime
                };

                _logger.LogInformation("Successfully generated optimization metrics and statistics");
                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting optimization metrics and statistics");
                return new OptimizationMetrics
                {
                    GeneratedAt = DateTimeOffset.UtcNow.DateTime
                };
            }
        }
    }
}