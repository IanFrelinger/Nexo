using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Factory.Models;

namespace Nexo.Feature.Factory.Interfaces;

/// <summary>
/// Optimization recommendation engine for usage pattern analysis and performance improvement suggestions
/// </summary>
public interface IOptimizationRecommendationEngine
{
    /// <summary>
    /// Implements usage pattern analysis for optimization insights
    /// </summary>
    /// <param name="analysisRequest">Usage pattern analysis request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Pattern analysis result</returns>
    Task<UsagePatternAnalysisResult> AnalyzeUsagePatternsAsync(UsagePatternAnalysisRequest analysisRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates optimization suggestion engine
    /// </summary>
    /// <param name="suggestionRequest">Optimization suggestion request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Optimization suggestions</returns>
    Task<OptimizationSuggestionResult> GenerateOptimizationSuggestionsAsync(OptimizationSuggestionRequest suggestionRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds performance improvement recommendations
    /// </summary>
    /// <param name="recommendationRequest">Performance recommendation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Performance recommendations</returns>
    Task<PerformanceRecommendationResult> GeneratePerformanceRecommendationsAsync(PerformanceRecommendationRequest recommendationRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates optimization reporting system
    /// </summary>
    /// <param name="reportRequest">Optimization report request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Optimization report</returns>
    Task<OptimizationReportResult> GenerateOptimizationReportAsync(OptimizationReportRequest reportRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets optimization metrics and trends
    /// </summary>
    /// <param name="metricsRequest">Optimization metrics request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Optimization metrics</returns>
    Task<OptimizationMetrics> GetOptimizationMetricsAsync(OptimizationMetricsRequest metricsRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies optimization recommendations
    /// </summary>
    /// <param name="applicationRequest">Optimization application request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Application result</returns>
    Task<OptimizationApplicationResult> ApplyOptimizationRecommendationsAsync(OptimizationApplicationRequest applicationRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates optimization recommendations
    /// </summary>
    /// <param name="validationRequest">Optimization validation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<OptimizationValidationResult> ValidateOptimizationRecommendationsAsync(OptimizationValidationRequest validationRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports optimization data
    /// </summary>
    /// <param name="exportRequest">Optimization export request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export result</returns>
    Task<OptimizationExportResult> ExportOptimizationDataAsync(OptimizationExportRequest exportRequest, CancellationToken cancellationToken = default);
} 