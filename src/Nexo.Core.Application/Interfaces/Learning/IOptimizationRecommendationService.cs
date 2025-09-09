using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Models.Learning;

namespace Nexo.Core.Application.Interfaces.Learning
{
    /// <summary>
    /// Interface for optimization recommendation service in Phase 9.
    /// Provides intelligent recommendations based on usage patterns and performance analysis.
    /// </summary>
    public interface IOptimizationRecommendationService
    {
        /// <summary>
        /// Implements usage pattern analysis for optimization recommendations.
        /// </summary>
        /// <param name="usagePatterns">The usage patterns to analyze</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Pattern analysis result</returns>
        Task<PatternAnalysisResult> AnalyzeUsagePatternsAsync(
            List<UsagePattern> usagePatterns,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates optimization suggestion engine.
        /// </summary>
        /// <param name="context">The context for optimization suggestions</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Optimization suggestions</returns>
        Task<OptimizationSuggestions> GenerateOptimizationSuggestionsAsync(
            OptimizationContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds performance improvement recommendations.
        /// </summary>
        /// <param name="performanceData">The performance data to analyze</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Performance recommendations</returns>
        Task<PerformanceRecommendations> GeneratePerformanceRecommendationsAsync(
            PerformanceData performanceData,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates optimization reporting system.
        /// </summary>
        /// <param name="reportOptions">The report options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Optimization report</returns>
        Task<OptimizationReport> GenerateOptimizationReportAsync(
            OptimizationReportOptions reportOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets optimization recommendations for specific features.
        /// </summary>
        /// <param name="featureId">The feature ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Feature optimization recommendations</returns>
        Task<FeatureOptimizationRecommendations> GetFeatureOptimizationRecommendationsAsync(
            string featureId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates optimization recommendations.
        /// </summary>
        /// <param name="recommendations">The recommendations to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<OptimizationValidationResult> ValidateOptimizationRecommendationsAsync(
            List<OptimizationRecommendation> recommendations,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies optimization recommendations.
        /// </summary>
        /// <param name="recommendations">The recommendations to apply</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Application result</returns>
        Task<OptimizationApplicationResult> ApplyOptimizationRecommendationsAsync(
            List<OptimizationRecommendation> recommendations,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets optimization metrics and statistics.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Optimization metrics</returns>
        Task<OptimizationMetrics> GetOptimizationMetricsAsync(
            CancellationToken cancellationToken = default);
    }
}
