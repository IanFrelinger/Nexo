using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Enums;

namespace Nexo.Feature.AI.Interfaces
{
    /// <summary>
    /// Interface for AI model orchestration and management.
    /// </summary>
    public interface IModelOrchestrator
    {
        /// <summary>
        /// Gets the available model providers.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of available model providers.</returns>
        Task<IEnumerable<IModelProvider>> GetProvidersAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Selects the best model for a given request.
        /// </summary>
        /// <param name="request">The model request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The selected model provider.</returns>
        Task<IModelProvider> SelectModelAsync(ModelRequest request, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Executes a model request.
        /// </summary>
        /// <param name="request">The model request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The model response.</returns>
        Task<ModelResponse> ExecuteAsync(ModelRequest request, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Validates a model request.
        /// </summary>
        /// <param name="request">The model request to validate.</param>
        /// <returns>Validation result.</returns>
        Task<ModelValidationResult> ValidateRequestAsync(ModelRequest request);

        /// <summary>
        /// Gets the health status of all model providers.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Health status of model providers.</returns>
        Task<IEnumerable<ModelHealthStatus>> GetHealthStatusAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Optimizes model selection based on performance metrics.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Optimization recommendations.</returns>
        Task<ModelOptimizationResult> OptimizeAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Registers a new model provider.
        /// </summary>
        /// <param name="provider">The model provider to register.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task RegisterProviderAsync(IModelProvider provider, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Unregisters a model provider.
        /// </summary>
        /// <param name="providerName">The name of the provider to unregister.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task UnregisterProviderAsync(string providerName, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the best available model for a specific task.
        /// </summary>
        /// <param name="task">The task description.</param>
        /// <param name="modelType">The preferred model type.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The best available model for the task.</returns>
        Task<IModelProvider> GetBestModelForTaskAsync(string task, ModelType modelType, CancellationToken cancellationToken = default(CancellationToken));
    }

    /// <summary>
    /// Interface for AI model providers.
    /// </summary>
    public interface IModelProvider
    {
        /// <summary>
        /// Gets the unique identifier of the provider.
        /// </summary>
        string ProviderId { get; }

        /// <summary>
        /// Gets the display name of the provider.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Gets the name of the provider.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the type of the provider.
        /// </summary>
        string ProviderType { get; }

        /// <summary>
        /// Gets whether the provider is enabled.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Gets whether the provider is the primary provider.
        /// </summary>
        bool IsPrimary { get; }

        /// <summary>
        /// Gets the supported model types.
        /// </summary>
        IEnumerable<ModelType> SupportedModelTypes { get; }

        /// <summary>
        /// Gets the model capabilities.
        /// </summary>
        ModelCapabilities Capabilities { get; }

        /// <summary>
        /// Gets available models from this provider.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of available models.</returns>
        Task<IEnumerable<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Loads a specific model.
        /// </summary>
        /// <param name="modelName">The name of the model to load.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The loaded model.</returns>
        Task<IModel> LoadModelAsync(string modelName, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Executes a model request.
        /// </summary>
        /// <param name="request">The model request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The model response.</returns>
        Task<ModelResponse> ExecuteAsync(ModelRequest request, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Validates a model request.
        /// </summary>
        /// <param name="request">The model request to validate.</param>
        /// <returns>Validation result.</returns>
        Task<ModelValidationResult> ValidateRequestAsync(ModelRequest request);

        /// <summary>
        /// Gets the health status of the provider.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The health status.</returns>
        Task<ModelHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default(CancellationToken));
    }


} 