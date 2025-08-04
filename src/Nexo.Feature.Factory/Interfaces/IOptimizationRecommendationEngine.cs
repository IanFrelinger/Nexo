using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

/// <summary>
/// Usage pattern analysis request
/// </summary>
public record UsagePatternAnalysisRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? ProjectId { get; init; }
    public string? UserId { get; init; }
    public List<string> PatternTypes { get; init; } = new();
    public bool EnablePredictiveAnalysis { get; init; }
    public bool EnableAnomalyDetection { get; init; }
    public Dictionary<string, object> AnalysisParameters { get; init; } = new();
}

/// <summary>
/// Usage pattern analysis result
/// </summary>
public record UsagePatternAnalysisResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<UsagePattern> DiscoveredPatterns { get; init; } = new();
    public List<UsageAnomaly> DetectedAnomalies { get; init; } = new();
    public List<UsageTrend> UsageTrends { get; init; } = new();
    public List<UsageInsight> Insights { get; init; } = new();
    public int PatternsAnalyzed { get; init; }
    public int AnomaliesDetected { get; init; }
    public double AnalysisAccuracy { get; init; }
    public TimeSpan AnalysisDuration { get; init; }
    public Dictionary<string, double> AnalysisMetrics { get; init; } = new();
    public DateTime AnalyzedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Usage anomaly
/// </summary>
public record UsageAnomaly
{
    public string AnomalyId { get; init; } = string.Empty;
    public string AnomalyType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double Severity { get; init; }
    public DateTime DetectedAt { get; init; }
    public List<string> AffectedComponents { get; init; } = new();
    public Dictionary<string, object> AnomalyData { get; init; } = new();
}

/// <summary>
/// Optimization suggestion request
/// </summary>
public record OptimizationSuggestionRequest
{
    public List<UsagePattern> UsagePatterns { get; init; } = new();
    public List<PerformanceMetric> PerformanceMetrics { get; init; } = new();
    public string OptimizationType { get; init; } = string.Empty;
    public bool EnableAIOptimization { get; init; }
    public bool EnableManualOptimization { get; init; }
    public Dictionary<string, object> SuggestionParameters { get; init; } = new();
}

/// <summary>
/// Performance metric
/// </summary>
public record PerformanceMetric
{
    public string MetricId { get; init; } = string.Empty;
    public string MetricName { get; init; } = string.Empty;
    public string MetricType { get; init; } = string.Empty;
    public double Value { get; init; }
    public string Unit { get; init; } = string.Empty;
    public DateTime MeasuredAt { get; init; }
    public Dictionary<string, object> MetricData { get; init; } = new();
}

/// <summary>
/// Optimization suggestion result
/// </summary>
public record OptimizationSuggestionResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<OptimizationSuggestion> Suggestions { get; init; } = new();
    public int SuggestionsGenerated { get; init; }
    public int HighPrioritySuggestions { get; init; }
    public double SuggestionQuality { get; init; }
    public TimeSpan GenerationDuration { get; init; }
    public Dictionary<string, double> GenerationMetrics { get; init; } = new();
    public DateTime GeneratedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Optimization suggestion
/// </summary>
public record OptimizationSuggestion
{
    public string SuggestionId { get; init; } = string.Empty;
    public string SuggestionType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public double Priority { get; init; }
    public double ExpectedImpact { get; init; }
    public string Effort { get; init; } = string.Empty;
    public List<string> AffectedComponents { get; init; } = new();
    public List<string> ImplementationSteps { get; init; } = new();
    public Dictionary<string, object> SuggestionData { get; init; } = new();
    public DateTime GeneratedAt { get; init; }
}

/// <summary>
/// Performance recommendation request
/// </summary>
public record PerformanceRecommendationRequest
{
    public List<PerformanceMetric> CurrentMetrics { get; init; } = new();
    public List<PerformanceTarget> PerformanceTargets { get; init; } = new();
    public string RecommendationType { get; init; } = string.Empty;
    public bool EnableAutomatedOptimization { get; init; }
    public Dictionary<string, object> RecommendationParameters { get; init; } = new();
}

/// <summary>
/// Performance target
/// </summary>
public record PerformanceTarget
{
    public string TargetId { get; init; } = string.Empty;
    public string MetricName { get; init; } = string.Empty;
    public double TargetValue { get; init; }
    public string Unit { get; init; } = string.Empty;
    public DateTime TargetDate { get; init; }
    public string Priority { get; init; } = string.Empty;
    public Dictionary<string, object> TargetData { get; init; } = new();
}

/// <summary>
/// Performance recommendation result
/// </summary>
public record PerformanceRecommendationResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<PerformanceRecommendation> Recommendations { get; init; } = new();
    public int RecommendationsGenerated { get; init; }
    public double OverallImprovementPotential { get; init; }
    public TimeSpan GenerationDuration { get; init; }
    public Dictionary<string, double> PerformanceMetrics { get; init; } = new();
    public DateTime GeneratedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Performance recommendation
/// </summary>
public record PerformanceRecommendation
{
    public string RecommendationId { get; init; } = string.Empty;
    public string MetricName { get; init; } = string.Empty;
    public double CurrentValue { get; init; }
    public double TargetValue { get; init; }
    public double ImprovementPotential { get; init; }
    public string Recommendation { get; init; } = string.Empty;
    public List<string> Actions { get; init; } = new();
    public double Priority { get; init; }
    public string Effort { get; init; } = string.Empty;
    public Dictionary<string, object> RecommendationData { get; init; } = new();
    public DateTime GeneratedAt { get; init; }
}

/// <summary>
/// Optimization report request
/// </summary>
public record OptimizationReportRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? ProjectId { get; init; }
    public List<string> ReportTypes { get; init; } = new();
    public string Format { get; init; } = string.Empty;
    public bool IncludeCharts { get; init; }
    public bool IncludeRecommendations { get; init; }
    public Dictionary<string, object> ReportParameters { get; init; } = new();
}

/// <summary>
/// Optimization report result
/// </summary>
public record OptimizationReportResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public string ReportPath { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public OptimizationSummary Summary { get; init; } = new();
    public List<OptimizationSection> Sections { get; init; } = new();
    public List<string> KeyFindings { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
    public DateTime GeneratedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Optimization summary
/// </summary>
public record OptimizationSummary
{
    public int TotalSuggestions { get; init; }
    public int ImplementedSuggestions { get; init; }
    public int PendingSuggestions { get; init; }
    public double OverallImprovement { get; init; }
    public double OptimizationEfficiency { get; init; }
    public string Status { get; init; } = string.Empty;
}

/// <summary>
/// Optimization section
/// </summary>
public record OptimizationSection
{
    public string SectionName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int SuggestionCount { get; init; }
    public int ImplementedCount { get; init; }
    public int PendingCount { get; init; }
    public double ImprovementRate { get; init; }
    public List<string> Issues { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
}

/// <summary>
/// Optimization metrics request
/// </summary>
public record OptimizationMetricsRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? ProjectId { get; init; }
    public List<string> MetricTypes { get; init; } = new();
    public bool IncludeTrends { get; init; }
    public Dictionary<string, object> MetricsParameters { get; init; } = new();
}

/// <summary>
/// Optimization metrics
/// </summary>
public record OptimizationMetrics
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public double OverallOptimizationScore { get; init; }
    public int TotalSuggestions { get; init; }
    public int ImplementedSuggestions { get; init; }
    public double ImplementationRate { get; init; }
    public double AverageImprovement { get; init; }
    public List<OptimizationTrend> OptimizationTrends { get; init; } = new();
    public Dictionary<string, double> DetailedMetrics { get; init; } = new();
    public DateTime GeneratedAt { get; init; }
}

/// <summary>
/// Optimization trend
/// </summary>
public record OptimizationTrend
{
    public string TrendId { get; init; } = string.Empty;
    public string MetricName { get; init; } = string.Empty;
    public string Direction { get; init; } = string.Empty;
    public double TrendStrength { get; init; }
    public List<TrendDataPoint> DataPoints { get; init; } = new();
    public DateTime TrendStart { get; init; }
    public DateTime TrendEnd { get; init; }
    public Dictionary<string, object> TrendData { get; init; } = new();
}

/// <summary>
/// Optimization application request
/// </summary>
public record OptimizationApplicationRequest
{
    public List<string> SuggestionIds { get; init; } = new();
    public string ApplicationType { get; init; } = string.Empty;
    public bool EnableValidation { get; init; }
    public bool EnableRollback { get; init; }
    public Dictionary<string, object> ApplicationParameters { get; init; } = new();
}

/// <summary>
/// Optimization application result
/// </summary>
public record OptimizationApplicationResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<AppliedOptimization> AppliedOptimizations { get; init; } = new();
    public int OptimizationsApplied { get; init; }
    public int OptimizationsFailed { get; init; }
    public double ApplicationSuccessRate { get; init; }
    public TimeSpan ApplicationDuration { get; init; }
    public Dictionary<string, double> ApplicationMetrics { get; init; } = new();
    public DateTime AppliedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Applied optimization
/// </summary>
public record AppliedOptimization
{
    public string SuggestionId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public double Impact { get; init; }
    public List<string> AppliedChanges { get; init; } = new();
    public DateTime AppliedAt { get; init; }
    public Dictionary<string, object> ApplicationData { get; init; } = new();
}

/// <summary>
/// Optimization validation request
/// </summary>
public record OptimizationValidationRequest
{
    public List<string> SuggestionIds { get; init; } = new();
    public string ValidationType { get; init; } = string.Empty;
    public bool EnableSimulation { get; init; }
    public bool EnableTesting { get; init; }
    public Dictionary<string, object> ValidationParameters { get; init; } = new();
}

/// <summary>
/// Optimization validation result
/// </summary>
public record OptimizationValidationResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<ValidatedOptimization> ValidatedOptimizations { get; init; } = new();
    public int OptimizationsValidated { get; init; }
    public int ValidOptimizations { get; init; }
    public int InvalidOptimizations { get; init; }
    public double ValidationSuccessRate { get; init; }
    public TimeSpan ValidationDuration { get; init; }
    public Dictionary<string, double> ValidationMetrics { get; init; } = new();
    public DateTime ValidatedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Validated optimization
/// </summary>
public record ValidatedOptimization
{
    public string SuggestionId { get; init; } = string.Empty;
    public bool IsValid { get; init; }
    public double ValidationScore { get; init; }
    public List<string> ValidationIssues { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
    public DateTime ValidatedAt { get; init; }
    public Dictionary<string, object> ValidationData { get; init; } = new();
}

/// <summary>
/// Optimization export request
/// </summary>
public record OptimizationExportRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string Format { get; init; } = string.Empty;
    public List<string> DataTypes { get; init; } = new();
    public bool IncludeMetadata { get; init; }
    public string? FilePath { get; init; }
}

/// <summary>
/// Optimization export result
/// </summary>
public record OptimizationExportResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public int DataRecordsExported { get; init; }
    public DateTime ExportedAt { get; init; }
    public string? ErrorMessage { get; init; }
} 