using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces
{
    /// <summary>
    /// Defines a processing queue for managing and dispatching asynchronous jobs.
    /// </summary>
    /// <typeparam name="TItem">The type of item in the queue.</typeparam>
    public interface IProcessingQueue<TItem>
    {
        /// <summary>
        /// Enqueues an item for processing.
        /// </summary>
        /// <param name="item">The item to enqueue.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task representing the enqueue operation.</returns>
        Task EnqueueAsync(TItem item, CancellationToken cancellationToken = default);

        /// <summary>
        /// Dequeues the next item for processing.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The next item in the queue.</returns>
        Task<TItem> DequeueAsync(CancellationToken cancellationToken = default);
    }
} 