using System;
using System.Threading;
using System.Threading.Tasks;

namespace NexoDirectorStudio.Commands
{
    /// <summary>
    /// Base interface for all commands in the Director Studio system.
    /// </summary>
    /// <typeparam name="TInput">The input type for the command</typeparam>
    /// <typeparam name="TOutput">The output type for the command</typeparam>
    public interface ICommand<in TInput, TOutput>
    {
        /// <summary>
        /// Executes the command with the given input.
        /// </summary>
        /// <param name="input">The input for the command</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The result of the command execution</returns>
        Task<TOutput> ExecuteAsync(TInput input, CancellationToken cancellationToken);
    }
}
