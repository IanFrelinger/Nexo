using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Pipeline.Models;
using ExecutionContext = Nexo.Feature.Pipeline.Models.ExecutionContext;

namespace Nexo.Feature.Pipeline.Interfaces
{
    /// <summary>
    /// Interface for knowledge base operations that store and retrieve learning insights.
    /// </summary>
    public interface IKnowledgeBase
    {
        /// <summary>
        /// Updates the knowledge base with execution results and patterns.
        /// </summary>
        /// <param name="result">The pipeline execution result.</param>
        /// <param name="patterns">The extracted patterns from the execution.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Task representing the update operation.</returns>
        Task UpdateWithExecutionResultAsync(
            PipelineExecutionResult result,
            Dictionary<string, object> patterns,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores a bottleneck pattern for future analysis.
        /// </summary>
        /// <param name="bottleneck">The bottleneck pattern to store.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Task representing the storage operation.</returns>
        Task StoreBottleneckPatternAsync(
            PerformanceBottleneck bottleneck,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores a strength pattern for future analysis.
        /// </summary>
        /// <param name="strength">The strength pattern to store.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Task representing the storage operation.</returns>
        Task StoreStrengthPatternAsync(
            PerformanceStrength strength,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores an optimization opportunity for future analysis.
        /// </summary>
        /// <param name="opportunity">The optimization opportunity to store.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Task representing the storage operation.</returns>
        Task StoreOptimizationOpportunityAsync(
            OptimizationOpportunity opportunity,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores user feedback for future analysis.
        /// </summary>
        /// <param name="feedback">The user feedback to store.</param>
        /// <param name="analysis">The feedback analysis.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Task representing the storage operation.</returns>
        Task StoreUserFeedbackAsync(
            UserFeedback feedback,
            Dictionary<string, object> analysis,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates user preferences based on feedback analysis.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="feedbackAnalysis">The feedback analysis.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Task representing the update operation.</returns>
        Task UpdateUserPreferencesAsync(
            string userId,
            Dictionary<string, object> feedbackAnalysis,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the adaptation state with new context and adaptations.
        /// </summary>
        /// <param name="context">The environment context.</param>
        /// <param name="adaptations">The applied adaptations.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Task representing the update operation.</returns>
        Task UpdateAdaptationStateAsync(
            EnvironmentContext context,
            List<AdaptationAction> adaptations,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves historical performance data for similar configurations.
        /// </summary>
        /// <param name="configuration">The pipeline configuration.</param>
        /// <param name="context">The execution context.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Historical performance data.</returns>
        Task<Dictionary<string, object>> GetHistoricalPerformanceAsync(
            PipelineConfiguration configuration,
            ExecutionContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves learning insights based on patterns.
        /// </summary>
        /// <param name="patternType">The type of pattern to search for.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Learning insights.</returns>
        Task<List<LearningInsight>> GetLearningInsightsAsync(
            string patternType,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves user preferences for a specific user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>User preferences.</returns>
        Task<Dictionary<string, object>> GetUserPreferencesAsync(
            string userId,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Learning insight extracted from patterns and historical data.
    /// </summary>
    public class LearningInsight
    {
        /// <summary>
        /// Gets or sets the insight identifier.
        /// </summary>
        public string InsightId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the insight type.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the insight description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the confidence level (0-100).
        /// </summary>
        public double ConfidenceLevel { get; set; }

        /// <summary>
        /// Gets or sets the insight data.
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the timestamp when this insight was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the source of this insight.
        /// </summary>
        public string Source { get; set; } = string.Empty;
    }
}
