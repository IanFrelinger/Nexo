using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Commands
{
    /// <summary>
    /// Base implementation for commands with common functionality
    /// </summary>
    /// <typeparam name="TInput">The input type for the command</typeparam>
    /// <typeparam name="TOutput">The output type for the command</typeparam>
    public abstract class BaseCommand<TInput, TOutput> : ICommand<TInput, TOutput>
    {
        public abstract string CommandId { get; }
        public abstract string CommandName { get; }
        public abstract string Description { get; }
        public virtual bool CanExecuteInParallel => true;
        public virtual string[] Dependencies => Array.Empty<string>();

        public abstract Task<CommandResult<TOutput>> ExecuteAsync(TInput input, CancellationToken cancellationToken = default);

        public virtual bool CanExecute(TInput input)
        {
            try
            {
                return ValidateInput(input);
            }
            catch
            {
                return false;
            }
        }

        public virtual async Task<CommandResult<TInput>> UndoAsync(TInput input, TOutput output, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                await UndoInternalAsync(input, output, cancellationToken);
                var duration = DateTime.UtcNow - startTime;
                
                return CommandResult<TInput>.Success(
                    input,
                    duration,
                    new[] { $"{CommandName} undo completed successfully" }
                );
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                return CommandResult<TInput>.Failure(
                    $"Failed to undo {CommandName}: {ex.Message}",
                    duration
                );
            }
        }

        /// <summary>
        /// Validates the input for this command
        /// </summary>
        protected virtual bool ValidateInput(TInput input)
        {
            return input != null;
        }

        /// <summary>
        /// Performs the actual undo logic
        /// </summary>
        protected virtual Task UndoInternalAsync(TInput input, TOutput output, CancellationToken cancellationToken)
        {
            // Default implementation does nothing (commands that don't modify state)
            return Task.CompletedTask;
        }

        /// <summary>
        /// Creates a successful result with timing
        /// </summary>
        protected CommandResult<TOutput> CreateSuccessResult(TOutput data, DateTime startTime, string[]? warnings = null, Dictionary<string, object>? metadata = null)
        {
            var duration = DateTime.UtcNow - startTime;
            return CommandResult<TOutput>.Success(data, duration, warnings, metadata);
        }

        /// <summary>
        /// Creates a failed result with timing
        /// </summary>
        protected CommandResult<TOutput> CreateFailureResult(string errorMessage, DateTime startTime, Dictionary<string, object>? metadata = null)
        {
            var duration = DateTime.UtcNow - startTime;
            return CommandResult<TOutput>.Failure(errorMessage, duration, metadata);
        }
    }
}
