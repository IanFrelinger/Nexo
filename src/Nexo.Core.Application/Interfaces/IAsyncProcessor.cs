using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces
{
    /// <summary>
    /// Defines an asynchronous processor for handling requests and producing responses.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    public interface IAsyncProcessor<TRequest, TResponse>
    {
        /// <summary>
        /// Processes the specified request asynchronously.
        /// </summary>
        /// <param name="request">The request to process.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The response produced by processing the request.</returns>
        Task<TResponse> ProcessAsync(TRequest request, CancellationToken cancellationToken = default);
    }
} 