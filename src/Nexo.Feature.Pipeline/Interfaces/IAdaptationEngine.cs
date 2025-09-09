using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.Feature.Pipeline.Interfaces
{
    /// <summary>
    /// Interface for adaptation engine that applies system adaptations and optimizations.
    /// </summary>
    public interface IAdaptationEngine
    {
        /// <summary>
        /// Updates adaptation strategies based on learned patterns.
        /// </summary>
        /// <param name="patterns">The learned patterns.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Task representing the update operation.</returns>
        Task UpdateStrategiesAsync(
            Dictionary<string, object> patterns,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies a specific adaptation to the system.
        /// </summary>
        /// <param name="adaptation">The adaptation to apply.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Task representing the adaptation operation.</returns>
        Task ApplyAdaptationAsync(
            AdaptationAction adaptation,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies a recommendation to the system.
        /// </summary>
        /// <param name="recommendation">The recommendation to apply.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Task representing the recommendation application.</returns>
        Task ApplyRecommendationAsync(
            AdaptationRecommendation recommendation,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Optimizes system configuration based on current state.
        /// </summary>
        /// <param name="currentState">The current system state.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Task representing the optimization operation.</returns>
        Task OptimizeSystemConfigurationAsync(
            AdaptationState currentState,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adjusts resource allocation based on current usage patterns.
        /// </summary>
        /// <param name="usagePatterns">The current usage patterns.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Task representing the resource adjustment.</returns>
        Task AdjustResourceAllocationAsync(
            Dictionary<string, object> usagePatterns,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current adaptation strategies.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Current adaptation strategies.</returns>
        Task<List<AdaptationStrategy>> GetCurrentStrategiesAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates an adaptation before applying it.
        /// </summary>
        /// <param name="adaptation">The adaptation to validate.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Validation result.</returns>
        Task<AdaptationValidationResult> ValidateAdaptationAsync(
            AdaptationAction adaptation,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Adaptation strategy for system optimization.
    /// </summary>
    public class AdaptationStrategy
    {
        /// <summary>
        /// Gets or sets the strategy identifier.
        /// </summary>
        public string StrategyId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the strategy name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the strategy description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the strategy type.
        /// </summary>
        public AdaptationType Type { get; set; }

        /// <summary>
        /// Gets or sets the strategy parameters.
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets whether the strategy is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the strategy effectiveness score (0-100).
        /// </summary>
        public double EffectivenessScore { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this strategy was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the timestamp when this strategy was last updated.
        /// </summary>
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Result of adaptation validation.
    /// </summary>
    public class AdaptationValidationResult
    {
        /// <summary>
        /// Gets or sets whether the adaptation is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the validation errors.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the validation warnings.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the estimated impact of the adaptation.
        /// </summary>
        public double EstimatedImpact { get; set; }

        /// <summary>
        /// Gets or sets the risk level of the adaptation.
        /// </summary>
        public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;

        /// <summary>
        /// Gets or sets the validation timestamp.
        /// </summary>
        public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Risk levels for adaptations.
    /// </summary>
    public enum RiskLevel
    {
        /// <summary>
        /// Low risk adaptation.
        /// </summary>
        Low,

        /// <summary>
        /// Medium risk adaptation.
        /// </summary>
        Medium,

        /// <summary>
        /// High risk adaptation.
        /// </summary>
        High,

        /// <summary>
        /// Critical risk adaptation.
        /// </summary>
        Critical
    }
}
