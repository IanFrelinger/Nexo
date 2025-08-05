using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Interfaces
{
    /// <summary>
    /// Interface for AI models that can process requests and generate responses.
    /// </summary>
    public interface IModel
    {
        /// <summary>
        /// Processes a request and generates a response.
        /// </summary>
        /// <param name="request">The request to process.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The generated response.</returns>
        Task<ModelResponse> ProcessAsync(ModelRequest request, CancellationToken cancellationToken = default);
    }


} 