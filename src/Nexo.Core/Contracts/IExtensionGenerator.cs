using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Contracts
{
    /// <summary>
    /// Interface for generating extensions/artifacts from requests.
    /// </summary>
    /// <typeparam name="TRequest">The type of request to process</typeparam>
    /// <typeparam name="TArtifact">The type of artifact to generate</typeparam>
    public interface IExtensionGenerator<TRequest, TArtifact>
    {
        /// <summary>
        /// Generates an artifact from the specified request.
        /// </summary>
        /// <param name="request">The request containing generation parameters</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>A generation result containing the artifact and metadata</returns>
        Task<GenerationResult<TArtifact>> GenerateAsync(TRequest request, CancellationToken ct = default);
    }
}
