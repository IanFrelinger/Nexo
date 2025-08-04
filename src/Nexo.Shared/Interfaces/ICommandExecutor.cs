using System;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Shared.Models;

namespace Nexo.Shared.Interfaces
{
    /// <summary>
    /// Defines the contract for executing system commands.
    /// </summary>
    public interface ICommandExecutor
    {
        /// <summary>
        /// Executes a command with arguments.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="arguments">The command arguments.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The command execution result.</returns>
        Task<CommandResult> ExecuteAsync(
            string command, 
            string arguments,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a command request.
        /// </summary>
        /// <param name="request">The command request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The command execution result.</returns>
        Task<CommandResult> ExecuteAsync(
            CommandRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a command and streams output.
        /// </summary>
        /// <param name="request">The command request.</param>
        /// <param name="outputCallback">Callback for output lines.</param>
        /// <param name="errorCallback">Callback for error lines.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The command execution result.</returns>
        Task<CommandResult> ExecuteWithCallbacksAsync(
            CommandRequest request,
            Action<string> outputCallback,
            Action<string> errorCallback,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a command is available.
        /// </summary>
        /// <param name="command">The command to check.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the command is available; otherwise, false.</returns>
        Task<bool> IsCommandAvailableAsync(
            string command,
            CancellationToken cancellationToken = default);
    }
}