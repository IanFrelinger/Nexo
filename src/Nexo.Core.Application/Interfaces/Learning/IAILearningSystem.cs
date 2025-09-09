using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Models.Learning;

namespace Nexo.Core.Application.Interfaces.Learning
{
    /// <summary>
    /// Interface for AI learning system in Phase 9.
    /// Enables continuous learning and improvement of the Feature Factory.
    /// </summary>
    public interface IAILearningSystem
    {
        /// <summary>
        /// Learns from feature patterns and usage data.
        /// </summary>
        /// <param name="featurePattern">The feature pattern to learn from</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Learning result</returns>
        Task<LearningResult> LearnFromFeaturePatternAsync(
            FeaturePattern featurePattern,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Accumulates domain knowledge from processed features.
        /// </summary>
        /// <param name="domainKnowledge">The domain knowledge to accumulate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Knowledge accumulation result</returns>
        Task<KnowledgeAccumulationResult> AccumulateDomainKnowledgeAsync(
            DomainKnowledge domainKnowledge,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes usage patterns to improve recommendations.
        /// </summary>
        /// <param name="usageData">The usage data to analyze</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Usage pattern analysis result</returns>
        Task<UsagePatternAnalysisResult> AnalyzeUsagePatternsAsync(
            UsageData usageData,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Implements learning feedback loops for continuous improvement.
        /// </summary>
        /// <param name="feedback">The feedback to process</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Feedback processing result</returns>
        Task<FeedbackProcessingResult> ProcessLearningFeedbackAsync(
            LearningFeedback feedback,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets learning insights and recommendations.
        /// </summary>
        /// <param name="context">The context for insights</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Learning insights</returns>
        Task<LearningInsights> GetLearningInsightsAsync(
            LearningContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the learning model with new data.
        /// </summary>
        /// <param name="learningData">The data to update the model with</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Model update result</returns>
        Task<ModelUpdateResult> UpdateLearningModelAsync(
            LearningData learningData,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates learning effectiveness and accuracy.
        /// </summary>
        /// <param name="validationData">The data to validate against</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Learning validation result</returns>
        Task<LearningValidationResult> ValidateLearningEffectivenessAsync(
            ValidationData validationData,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Exports learning data for analysis and backup.
        /// </summary>
        /// <param name="exportOptions">The export options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Learning data export</returns>
        Task<LearningDataExport> ExportLearningDataAsync(
            LearningDataExportOptions exportOptions,
            CancellationToken cancellationToken = default);
    }
}
