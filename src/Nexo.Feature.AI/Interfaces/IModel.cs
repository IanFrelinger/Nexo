using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Interfaces;

/// <summary>
/// Interface for AI models that can process requests and generate responses
/// </summary>
public interface IModel
{
    /// <summary>
    /// Unique identifier for this model instance
    /// </summary>
    string ModelId { get; }
    
    /// <summary>
    /// Human-readable name of the model
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Type of model
    /// </summary>
    Enums.ModelType ModelType { get; }
    
    /// <summary>
    /// Whether the model is currently loaded and ready
    /// </summary>
    bool IsLoaded { get; }
    
    /// <summary>
    /// Processes a request and generates a response
    /// </summary>
    /// <param name="request">The request to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The generated response</returns>
    Task<ModelResponse> ProcessAsync(ModelRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Loads the model into memory
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the load operation</returns>
    Task LoadAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Unloads the model from memory
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the unload operation</returns>
    Task UnloadAsync(CancellationToken cancellationToken = default);
}
