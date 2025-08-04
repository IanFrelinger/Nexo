using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces;

namespace Nexo.Core.Application.Services
{
    /// <summary>
    /// In-memory thread-safe processing queue implementation.
    /// </summary>
    /// <typeparam name="TItem">The type of item in the queue.</typeparam>
    public class ProcessingQueue<TItem> : IProcessingQueue<TItem>
    {
        private readonly ConcurrentQueue<TItem> _queue = new ConcurrentQueue<TItem>();
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);

        /// <inheritdoc />
        public Task EnqueueAsync(TItem item, CancellationToken cancellationToken = default)
        {
            _queue.Enqueue(item);
            _signal.Release();
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task<TItem> DequeueAsync(CancellationToken cancellationToken = default)
        {
            await _signal.WaitAsync(cancellationToken);
            if (_queue.TryDequeue(out var item))
            {
                return item;
            }
            throw new InvalidOperationException("Queue was signaled but no item was available.");
        }
    }
} 