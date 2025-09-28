using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Commands
{
    /// <summary>
    /// Base interface for all commands in the system
    /// </summary>
    /// <typeparam name="TInput">The input type for the command</typeparam>
    /// <typeparam name="TOutput">The output type for the command</typeparam>
    public interface ICommand<TInput, TOutput>
    {
        /// <summary>
        /// Gets the unique identifier for this command
        /// </summary>
        string CommandId { get; }

        /// <summary>
        /// Gets the display name for this command
        /// </summary>
        string CommandName { get; }

        /// <summary>
        /// Gets the description of what this command does
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets whether this command can be executed in parallel with other commands
        /// </summary>
        bool CanExecuteInParallel { get; }

        /// <summary>
        /// Gets the dependencies for this command (command IDs that must complete first)
        /// </summary>
        string[] Dependencies { get; }

        /// <summary>
        /// Executes the command
        /// </summary>
        /// <param name="input">The input for the command</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The result of the command execution</returns>
        Task<CommandResult<TOutput>> ExecuteAsync(TInput input, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates that the command can be executed with the given input
        /// </summary>
        /// <param name="input">The input to validate</param>
        /// <returns>True if the command can be executed, false otherwise</returns>
        bool CanExecute(TInput input);

        /// <summary>
        /// Undoes the changes made by this command
        /// </summary>
        /// <param name="input">The original input</param>
        /// <param name="output">The output that was produced</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The result of the undo operation</returns>
        Task<CommandResult<TInput>> UndoAsync(TInput input, TOutput output, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of a command execution
    /// </summary>
    /// <typeparam name="T">The result type</typeparam>
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
        /// Gets or sets any error message if the command failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets any warnings generated during execution
        /// </summary>
        public string[] Warnings { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets additional metadata about the command execution
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the execution duration
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Creates a successful result
        /// </summary>
        public static CommandResult<T> Success(T data, TimeSpan duration, string[]? warnings = null, Dictionary<string, object>? metadata = null)
        {
            return new CommandResult<T>
            {
                IsSuccess = true,
                Data = data,
                Duration = duration,
                Warnings = warnings ?? Array.Empty<string>(),
                Metadata = metadata ?? new Dictionary<string, object>()
            };
        }

        /// <summary>
        /// Creates a failed result
        /// </summary>
        public static CommandResult<T> Failure(string errorMessage, TimeSpan duration, Dictionary<string, object>? metadata = null)
        {
            return new CommandResult<T>
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                Duration = duration,
                Metadata = metadata ?? new Dictionary<string, object>()
            };
        }
    }
}
