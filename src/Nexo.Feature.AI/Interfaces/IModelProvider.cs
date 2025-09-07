using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Interfaces;

/// <summary>
/// Interface for providers that can supply AI models
/// </summary>
public interface IModelProvider
{
    /// <summary>
    /// Unique identifier for this provider
    /// </summary>
    string ProviderId { get; }
    
    /// <summary>
    /// Human-readable display name
    /// </summary>
    string DisplayName { get; }
    
    /// <summary>
    /// Short name for the provider
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Types of models this provider supports
    /// </summary>
    IEnumerable<Enums.ModelType> SupportedModelTypes { get; }
    
    /// <summary>
    /// Gets all available models from this provider
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of available models</returns>
    Task<IEnumerable<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Loads a specific model by name
    /// </summary>
    /// <param name="modelName">Name of the model to load</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The loaded model instance</returns>
    Task<IModel> LoadModelAsync(string modelName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes a request using the provider's default model
    /// </summary>
    /// <param name="request">The request to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The response from the model</returns>
    Task<ModelResponse> ExecuteAsync(ModelRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the health status of this provider
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health status information</returns>
    Task<ModelHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default);
}
