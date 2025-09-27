using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Factory.Interfaces;

/// <summary>
/// AI learning system for continuous improvement and pattern recognition in Feature Factory.
/// This interface acts as an orchestrator, delegating specific functionalities to partial interface implementations.
/// </summary>
public partial interface IAILearningSystem
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
// This interface acts as an orchestrator for various AI learning functionalities,
// with specific categories defined in partial interfaces.