using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Interfaces
{
    /// <summary>
    /// Advanced model orchestrator interface for intelligent model selection, fallback strategies,
    /// and multi-model coordination capabilities.
    /// </summary>
    public interface IAdvancedModelOrchestrator
    {
        /// <summary>
        /// Executes a multimodel workflow with coordination between different models.
        /// </summary>
        /// <param name="workflow">The multimodel workflow definition.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Multimodel workflow response with step results.</returns>
        Task<MultiModelResponse> ExecuteMultiModelWorkflowAsync(MultiModelWorkflow workflow, CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes and optimizes model performance based on historical data.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Model optimization analysis and recommendations.</returns>
        Task<ModelOptimizationResult> AnalyzeAndOptimizeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a custom model selection rule.
        /// </summary>
        /// <param name="rule">The model selection rule to add.</param>
        void AddSelectionRule(ModelSelectionRule rule);

        /// <summary>
        /// Updates model capabilities profile.
        /// </summary>
        /// <param name="modelName">The name of the model.</param>
        /// <param name="capabilities">The updated capabilities profile.</param>
        void UpdateModelCapabilities(string modelName, ModelCapabilityProfile capabilities);
    }
} 