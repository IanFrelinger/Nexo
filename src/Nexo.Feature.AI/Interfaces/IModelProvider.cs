using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Enums;

namespace Nexo.Feature.AI.Interfaces
{


    /// <summary>
    /// Interface for AI models that can process requests and generate responses.
    /// </summary>
    public interface IModel
    {
        /// <summary>
        /// Gets the model information.
        /// </summary>
        ModelInfo Info { get; }

        /// <summary>
        /// Gets whether the model is loaded and ready to use.
        /// </summary>
        bool IsLoaded { get; }

        /// <summary>
        /// Processes a request and generates a response.
        /// </summary>
        /// <param name="request">The request to process.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The generated response.</returns>
        Task<ModelResponse> ProcessAsync(ModelRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes a request with streaming response.
        /// </summary>
        /// <param name="request">The request to process.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Streaming response.</returns>
        IEnumerable<ModelResponseChunk> ProcessStreamAsync(ModelRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the model's capabilities.
        /// </summary>
        /// <returns>Model capabilities.</returns>
        ModelCapabilities GetCapabilities();

        /// <summary>
        /// Unloads the model and releases resources.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UnloadAsync(CancellationToken cancellationToken = default);
    }


} 