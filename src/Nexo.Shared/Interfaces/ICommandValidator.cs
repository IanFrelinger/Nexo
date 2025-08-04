using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Nexo.Shared.Models;

namespace Nexo.Shared.Interfaces
{
    /// <summary>
    /// Validates commands before execution.
    /// </summary>
    public interface ICommandValidator
    {
        /// <summary>
        /// Validates a command.
        /// </summary>
        /// <param name="command">The command to validate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The validation result.</returns>
        Task<ValidationResult> ValidateAsync(
            string[] command,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the list of allowed commands.
        /// </summary>
        /// <returns>A collection of allowed command names.</returns>
        IEnumerable<string> GetAllowedCommands();

        /// <summary>
        /// Gets the list of forbidden commands.
        /// </summary>
        /// <returns>A collection of forbidden command names.</returns>
        IEnumerable<string> GetForbiddenCommands();

        /// <summary>
        /// Adds an allowed command.
        /// </summary>
        /// <param name="command">The command to allow.</param>
        void AllowCommand(string command);

        /// <summary>
        /// Adds a forbidden command.
        /// </summary>
        /// <param name="command">The command to forbid.</param>
        void ForbidCommand(string command);

        /// <summary>
        /// Resets to default validation rules.
        /// </summary>
        void ResetToDefaults();
    }
} 