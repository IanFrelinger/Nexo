using System;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces;

namespace Nexo.Core.Application.Services
{
    /// <summary>
    /// Basic asynchronous processor implementation using a handler function.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    public class AsyncProcessor<TRequest, TResponse> : IAsyncProcessor<TRequest, TResponse>
    {
        private readonly Func<TRequest, CancellationToken, Task<TResponse>> _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncProcessor{TRequest, TResponse}"/> class.
        /// </summary>
        /// <param name="handler">The handler function to process requests.</param>
        public AsyncProcessor(Func<TRequest, CancellationToken, Task<TResponse>> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        /// <inheritdoc />
        public Task<TResponse> ProcessAsync(TRequest request, CancellationToken cancellationToken = default)
        {
            return _handler(request, cancellationToken);
        }
    }
} 