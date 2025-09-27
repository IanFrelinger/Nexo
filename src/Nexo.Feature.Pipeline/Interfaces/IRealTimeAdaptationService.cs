using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Pipeline.Models;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Feature.Pipeline.Interfaces
{
    /// <summary>
    /// Interface for real-time adaptation services that enable continuous learning and optimization.
    /// This interface acts as an orchestrator, delegating specific functionalities to partial interface implementations.
    /// </summary>
    public partial interface IRealTimeAdaptationService
    {
        /// <summary>
        /// Learns from pipeline execution results to improve future performance.
        /// </summary>
        /// <param name="result">The pipeline execution result to learn from.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Task representing the learning operation.</returns>
        Task LearnFromExecutionAsync(
            PipelineExecutionResult result,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adapts the system to the current environment context.
        /// </summary>
        /// <param name="context">The environment context to adapt to.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Task representing the adaptation operation.</returns>
        Task AdaptToEnvironmentAsync(
            EnvironmentContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets adaptation recommendations based on current system state.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>List of adaptation recommendations.</returns>
        Task<List<AdaptationRecommendation>> GetRecommendationsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes user feedback to improve system behavior.
        /// </summary>
        /// <param name="feedback">The user feedback to process.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Task representing the feedback processing operation.</returns>
        Task ProcessUserFeedbackAsync(
            UserFeedback feedback,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current adaptation state of the system.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Current adaptation state information.</returns>
        Task<AdaptationState> GetAdaptationStateAsync(
            CancellationToken cancellationToken = default);
        // This interface acts as an orchestrator for various real-time adaptation functionalities,
        // with specific categories defined in partial interfaces.
    }
}