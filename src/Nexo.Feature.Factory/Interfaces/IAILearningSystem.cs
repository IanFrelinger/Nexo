using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Factory.Interfaces;

/// <summary>
/// AI learning system for continuous improvement and pattern recognition in Feature Factory
/// </summary>
public interface IAILearningSystem
{
    /// <summary>
    /// Learns from feature patterns and improves AI capabilities
    /// </summary>
    /// <param name="learningRequest">Feature pattern learning request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Learning result</returns>
    Task<FeaturePatternLearningResult> LearnFromFeaturePatternsAsync(FeaturePatternLearningRequest learningRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Accumulates domain knowledge from processed features
    /// </summary>
    /// <param name="knowledgeRequest">Domain knowledge accumulation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Knowledge accumulation result</returns>
    Task<DomainKnowledgeAccumulationResult> AccumulateDomainKnowledgeAsync(DomainKnowledgeAccumulationRequest knowledgeRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes usage patterns to improve AI performance
    /// </summary>
    /// <param name="analysisRequest">Usage pattern analysis request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Pattern analysis result</returns>
    Task<UsagePatternAnalysisResult> AnalyzeUsagePatternsAsync(UsagePatternAnalysisRequest analysisRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Implements learning feedback loops for continuous improvement
    /// </summary>
    /// <param name="feedbackRequest">Learning feedback request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Feedback processing result</returns>
    Task<LearningFeedbackResult> ProcessLearningFeedbackAsync(LearningFeedbackRequest feedbackRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets AI learning metrics and performance data
    /// </summary>
    /// <param name="metricsRequest">Learning metrics request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Learning metrics</returns>
    Task<AILearningMetrics> GetLearningMetricsAsync(AILearningMetricsRequest metricsRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Optimizes AI models based on learning data
    /// </summary>
    /// <param name="optimizationRequest">Model optimization request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Optimization result</returns>
    Task<ModelOptimizationResult> OptimizeAIModelsAsync(ModelOptimizationRequest optimizationRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports learning data for analysis
    /// </summary>
    /// <param name="exportRequest">Learning data export request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export result</returns>
    Task<LearningDataExportResult> ExportLearningDataAsync(LearningDataExportRequest exportRequest, CancellationToken cancellationToken = default);
}

/// <summary>
/// Feature pattern learning request
/// </summary>
public record FeaturePatternLearningRequest
{
    public List<FeaturePattern> Patterns { get; init; } = new();
    public string Domain { get; init; } = string.Empty;
    public string Industry { get; init; } = string.Empty;
    public LearningMode Mode { get; init; }
    public bool EnableRealTimeLearning { get; init; }
    public Dictionary<string, object> LearningParameters { get; init; } = new();
}

/// <summary>
/// Learning modes
/// </summary>
public enum LearningMode
{
    Supervised,
    Unsupervised,
    Reinforcement,
    Transfer,
    Active
}

/// <summary>
/// Feature pattern
/// </summary>
public record FeaturePattern
{
    public string PatternId { get; init; } = string.Empty;
    public string PatternType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<string> Keywords { get; init; } = new();
    public List<string> Components { get; init; } = new();
    public Dictionary<string, object> Attributes { get; init; } = new();
    public double Confidence { get; init; }
    public DateTime DiscoveredAt { get; init; }
    public int UsageCount { get; init; }
}

/// <summary>
/// Feature pattern learning result
/// </summary>
public record FeaturePatternLearningResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<LearnedPattern> LearnedPatterns { get; init; } = new();
    public int PatternsProcessed { get; init; }
    public int NewPatternsDiscovered { get; init; }
    public int PatternsImproved { get; init; }
    public double LearningAccuracy { get; init; }
    public TimeSpan LearningDuration { get; init; }
    public Dictionary<string, double> PerformanceMetrics { get; init; } = new();
    public DateTime LearnedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Learned pattern
/// </summary>
public record LearnedPattern
{
    public string PatternId { get; init; } = string.Empty;
    public string PatternType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double Confidence { get; init; }
    public double Accuracy { get; init; }
    public int TrainingExamples { get; init; }
    public List<string> Applications { get; init; } = new();
    public Dictionary<string, object> Metadata { get; init; } = new();
    public DateTime LearnedAt { get; init; }
}

/// <summary>
/// Domain knowledge accumulation request
/// </summary>
public record DomainKnowledgeAccumulationRequest
{
    public List<DomainKnowledge> KnowledgeItems { get; init; } = new();
    public string Domain { get; init; } = string.Empty;
    public string Industry { get; init; } = string.Empty;
    public bool EnableKnowledgeGraph { get; init; }
    public bool EnableSemanticSearch { get; init; }
    public Dictionary<string, object> AccumulationParameters { get; init; } = new();
}

/// <summary>
/// Domain knowledge
/// </summary>
public record DomainKnowledge
{
    public string KnowledgeId { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public List<string> Tags { get; init; } = new();
    public List<string> RelatedConcepts { get; init; } = new();
    public double Relevance { get; init; }
    public DateTime AcquiredAt { get; init; }
    public string Source { get; init; } = string.Empty;
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Domain knowledge accumulation result
/// </summary>
public record DomainKnowledgeAccumulationResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<AccumulatedKnowledge> AccumulatedKnowledge { get; init; } = new();
    public int KnowledgeItemsProcessed { get; init; }
    public int NewKnowledgeItems { get; init; }
    public int UpdatedKnowledgeItems { get; init; }
    public double KnowledgeCoverage { get; init; }
    public TimeSpan AccumulationDuration { get; init; }
    public Dictionary<string, double> CoverageMetrics { get; init; } = new();
    public DateTime AccumulatedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Accumulated knowledge
/// </summary>
public record AccumulatedKnowledge
{
    public string KnowledgeId { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public double Confidence { get; init; }
    public int ReferenceCount { get; init; }
    public DateTime LastUpdated { get; init; }
    public List<string> RelatedKnowledge { get; init; } = new();
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Usage pattern analysis request
/// </summary>
public record UsagePatternAnalysisRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? Domain { get; init; }
    public string? Industry { get; init; }
    public List<string> AnalysisTypes { get; init; } = new();
    public bool EnablePredictiveAnalysis { get; init; }
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
    public List<UsageTrend> UsageTrends { get; init; } = new();
    public List<UsageInsight> Insights { get; init; } = new();
    public int PatternsAnalyzed { get; init; }
    public int NewPatternsFound { get; init; }
    public double AnalysisAccuracy { get; init; }
    public TimeSpan AnalysisDuration { get; init; }
    public Dictionary<string, double> AnalysisMetrics { get; init; } = new();
    public DateTime AnalyzedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Usage pattern
/// </summary>
public record UsagePattern
{
    public string PatternId { get; init; } = string.Empty;
    public string PatternType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double Frequency { get; init; }
    public double Confidence { get; init; }
    public List<string> Users { get; init; } = new();
    public List<string> Features { get; init; } = new();
    public DateTime FirstSeen { get; init; }
    public DateTime LastSeen { get; init; }
    public Dictionary<string, object> PatternData { get; init; } = new();
}

/// <summary>
/// Usage trend
/// </summary>
public record UsageTrend
{
    public string TrendId { get; init; } = string.Empty;
    public string TrendType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double TrendStrength { get; init; }
    public string Direction { get; init; } = string.Empty;
    public List<TrendDataPoint> DataPoints { get; init; } = new();
    public DateTime TrendStart { get; init; }
    public DateTime TrendEnd { get; init; }
    public Dictionary<string, object> TrendData { get; init; } = new();
}

/// <summary>
/// Trend data point
/// </summary>
public record TrendDataPoint
{
    public DateTime Timestamp { get; init; }
    public double Value { get; init; }
    public string Unit { get; init; } = string.Empty;
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Usage insight
/// </summary>
public record UsageInsight
{
    public string InsightId { get; init; } = string.Empty;
    public string InsightType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double Confidence { get; init; }
    public string Impact { get; init; } = string.Empty;
    public List<string> Recommendations { get; init; } = new();
    public DateTime DiscoveredAt { get; init; }
    public Dictionary<string, object> InsightData { get; init; } = new();
}

/// <summary>
/// Learning feedback request
/// </summary>
public record LearningFeedbackRequest
{
    public List<LearningFeedback> FeedbackItems { get; init; } = new();
    public string FeedbackType { get; init; } = string.Empty;
    public bool EnableImmediateLearning { get; init; }
    public bool EnableBatchLearning { get; init; }
    public Dictionary<string, object> FeedbackParameters { get; init; } = new();
}

/// <summary>
/// Learning feedback
/// </summary>
public record LearningFeedback
{
    public string FeedbackId { get; init; } = string.Empty;
    public string FeatureId { get; init; } = string.Empty;
    public string FeedbackType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double Rating { get; init; }
    public List<string> Tags { get; init; } = new();
    public DateTime ProvidedAt { get; init; }
    public string UserId { get; init; } = string.Empty;
    public Dictionary<string, object> FeedbackData { get; init; } = new();
}

/// <summary>
/// Learning feedback result
/// </summary>
public record LearningFeedbackResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<ProcessedFeedback> ProcessedFeedback { get; init; } = new();
    public int FeedbackItemsProcessed { get; init; }
    public int LearningUpdatesApplied { get; init; }
    public double LearningImprovement { get; init; }
    public TimeSpan ProcessingDuration { get; init; }
    public Dictionary<string, double> ImprovementMetrics { get; init; } = new();
    public DateTime ProcessedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Processed feedback
/// </summary>
public record ProcessedFeedback
{
    public string FeedbackId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public double Impact { get; init; }
    public List<string> AppliedChanges { get; init; } = new();
    public DateTime ProcessedAt { get; init; }
    public Dictionary<string, object> ProcessingData { get; init; } = new();
}

/// <summary>
/// AI learning metrics request
/// </summary>
public record AILearningMetricsRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? ModelType { get; init; }
    public List<string> Metrics { get; init; } = new();
    public bool IncludeTrends { get; init; }
    public Dictionary<string, object> MetricsParameters { get; init; } = new();
}

/// <summary>
/// AI learning metrics
/// </summary>
public record AILearningMetrics
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public double OverallAccuracy { get; init; }
    public double LearningRate { get; init; }
    public int PatternsLearned { get; init; }
    public int KnowledgeItemsAccumulated { get; init; }
    public double ImprovementRate { get; init; }
    public List<ModelPerformance> ModelPerformance { get; init; } = new();
    public List<LearningTrend> LearningTrends { get; init; } = new();
    public Dictionary<string, double> DetailedMetrics { get; init; } = new();
    public DateTime GeneratedAt { get; init; }
}

/// <summary>
/// Model performance
/// </summary>
public record ModelPerformance
{
    public string ModelId { get; init; } = string.Empty;
    public string ModelType { get; init; } = string.Empty;
    public double Accuracy { get; init; }
    public double Precision { get; init; }
    public double Recall { get; init; }
    public double F1Score { get; init; }
    public int TrainingExamples { get; init; }
    public DateTime LastUpdated { get; init; }
    public Dictionary<string, double> PerformanceData { get; init; } = new();
}

/// <summary>
/// Learning trend
/// </summary>
public record LearningTrend
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
/// Model optimization request
/// </summary>
public record ModelOptimizationRequest
{
    public List<string> ModelIds { get; init; } = new();
    public string OptimizationType { get; init; } = string.Empty;
    public bool EnableHyperparameterTuning { get; init; }
    public bool EnableArchitectureOptimization { get; init; }
    public Dictionary<string, object> OptimizationParameters { get; init; } = new();
}

/// <summary>
/// Model optimization result
/// </summary>
public record ModelOptimizationResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<OptimizedModel> OptimizedModels { get; init; } = new();
    public int ModelsOptimized { get; init; }
    public double AverageImprovement { get; init; }
    public TimeSpan OptimizationDuration { get; init; }
    public Dictionary<string, double> OptimizationMetrics { get; init; } = new();
    public DateTime OptimizedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Optimized model
/// </summary>
public record OptimizedModel
{
    public string ModelId { get; init; } = string.Empty;
    public string ModelType { get; init; } = string.Empty;
    public double BeforeAccuracy { get; init; }
    public double AfterAccuracy { get; init; }
    public double Improvement { get; init; }
    public List<string> OptimizationsApplied { get; init; } = new();
    public DateTime OptimizedAt { get; init; }
    public Dictionary<string, object> OptimizationData { get; init; } = new();
}

/// <summary>
/// Learning data export request
/// </summary>
public record LearningDataExportRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string Format { get; init; } = string.Empty;
    public List<string> DataTypes { get; init; } = new();
    public bool IncludeMetadata { get; init; }
    public string? FilePath { get; init; }
}

/// <summary>
/// Learning data export result
/// </summary>
public record LearningDataExportResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public int DataRecordsExported { get; init; }
    public DateTime ExportedAt { get; init; }
    public string? ErrorMessage { get; init; }
} 