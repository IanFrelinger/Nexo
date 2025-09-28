using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Commands
{
    /// <summary>
    /// Simplified command interface following the test structure principles:
    /// - Single responsibility
    /// - Simple execution
    /// - Clear input/output
    /// - No complex orchestration
    /// </summary>
    /// <typeparam name="TInput">The input type for the command</typeparam>
    /// <typeparam name="TOutput">The output type for the command</typeparam>
    public interface ISimplifiedCommand<TInput, TOutput>
    {
        /// <summary>
        /// Gets the name of this command
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Executes the command with simple, focused logic
        /// </summary>
        /// <param name="input">The input for the command</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The result of the command execution</returns>
        Task<CommandResult<TOutput>> ExecuteAsync(TInput input, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Simplified base command implementation
    /// </summary>
    /// <typeparam name="TInput">The input type for the command</typeparam>
    /// <typeparam name="TOutput">The output type for the command</typeparam>
    public abstract class SimplifiedCommand<TInput, TOutput> : ISimplifiedCommand<TInput, TOutput>
    {
        public abstract string Name { get; }

        public abstract Task<CommandResult<TOutput>> ExecuteAsync(TInput input, CancellationToken cancellationToken = default);

        /// <summary>
        /// Helper method to create success results (like test assertions)
        /// </summary>
        protected static CommandResult<TOutput> Success(TOutput output, TimeSpan duration, string[]? warnings = null)
        {
            return CommandResult<TOutput>.Success(output, duration, warnings);
        }

        /// <summary>
        /// Helper method to create failure results (like test failures)
        /// </summary>
        protected static CommandResult<TOutput> Failure(string errorMessage, TimeSpan duration)
        {
            return CommandResult<TOutput>.Failure(errorMessage, duration);
        }
    }
}
