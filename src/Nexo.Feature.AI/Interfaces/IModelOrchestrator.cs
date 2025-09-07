using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Interfaces;

/// <summary>
/// Interface for orchestrating AI model operations
/// </summary>
public interface IModelOrchestrator
{
    /// <summary>
    /// Executes a request using the best available model
    /// </summary>
    /// <param name="request">The request to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The response from the model</returns>
    Task<ModelResponse> ExecuteAsync(ModelRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Registers a new model provider
    /// </summary>
    /// <param name="provider">The provider to register</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the registration operation</returns>
    Task RegisterProviderAsync(IModelProvider provider, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the best model provider for a specific task
    /// </summary>
    /// <param name="task">Description of the task</param>
    /// <param name="modelType">Type of model needed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The best provider for the task</returns>
    Task<IModelProvider> GetBestModelForTaskAsync(string task, Enums.ModelType modelType, CancellationToken cancellationToken = default);
}
