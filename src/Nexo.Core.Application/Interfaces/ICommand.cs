using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces
{
    /// <summary>
    /// Represents a command that can be executed
    /// </summary>
    /// <typeparam name="TInput">The input type for the command</typeparam>
    /// <typeparam name="TOutput">The output type for the command</typeparam>
    public interface ICommand<TInput, TOutput>
    {
        /// <summary>
        /// Gets the unique identifier for this command
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the name of this command
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the description of this command
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Executes the command with the given input
        /// </summary>
        /// <param name="input">The input for the command</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The result of the command execution</returns>
        Task<CommandResult<TOutput>> ExecuteAsync(TInput input, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents the result of a command execution
    /// </summary>
    /// <typeparam name="T">The type of the result data</typeparam>
    public class CommandResult<T>
    {
        /// <summary>
        /// Gets or sets whether the command execution was successful
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Gets or sets the result data
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// Gets or sets the error message if the command failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the execution time
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }

        /// <summary>
        /// Gets or sets any warnings that occurred during execution
        /// </summary>
        public string[]? Warnings { get; set; }

        /// <summary>
        /// Gets or sets additional metadata about the execution
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Creates a successful result
        /// </summary>
        public static CommandResult<T> Success(T data, TimeSpan executionTime, string[]? warnings = null, Dictionary<string, object>? metadata = null)
        {
            return new CommandResult<T>
            {
                IsSuccess = true,
                Data = data,
                ExecutionTime = executionTime,
                Warnings = warnings,
                Metadata = metadata
            };
        }

        /// <summary>
        /// Creates a failed result
        /// </summary>
        public static CommandResult<T> Failure(string errorMessage, TimeSpan executionTime, Dictionary<string, object>? metadata = null)
        {
            return new CommandResult<T>
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                ExecutionTime = executionTime,
                Metadata = metadata
            };
        }
    }
}
