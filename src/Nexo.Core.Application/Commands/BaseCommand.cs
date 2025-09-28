using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces;

namespace Nexo.Core.Application.Commands
{
    /// <summary>
    /// Base implementation for commands
    /// </summary>
    /// <typeparam name="TInput">The input type for the command</typeparam>
    /// <typeparam name="TOutput">The output type for the command</typeparam>
    public abstract class BaseCommand<TInput, TOutput> : ICommand<TInput, TOutput>
    {
        public abstract string Id { get; }
        public abstract string Name { get; }
        public abstract string Description { get; }

        /// <summary>
        /// Executes the command with timing and error handling
        /// </summary>
        public async Task<CommandResult<TOutput>> ExecuteAsync(TInput input, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Validate input
                var validationResult = ValidateInput(input);
                if (!validationResult.IsValid)
                {
                    return CommandResult<TOutput>.Failure(
                        $"Input validation failed: {string.Join(", ", validationResult.Errors)}",
                        stopwatch.Elapsed);
                }

                // Execute the command
                var result = await ExecuteInternalAsync(input, cancellationToken);
                
                stopwatch.Stop();
                return CommandResult<TOutput>.Success(result, stopwatch.Elapsed);
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                return CommandResult<TOutput>.Failure("Command was cancelled", stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return CommandResult<TOutput>.Failure($"Command execution failed: {ex.Message}", stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Validates the input for the command
        /// </summary>
        protected virtual CommandValidationResult ValidateInput(TInput input)
        {
            if (input == null)
            {
                return new CommandValidationResult
                {
                    IsValid = false,
                    Errors = new[] { "Input cannot be null" }
                };
            }

            return new CommandValidationResult { IsValid = true };
        }

        /// <summary>
        /// Executes the command logic
        /// </summary>
        protected abstract Task<TOutput> ExecuteInternalAsync(TInput input, CancellationToken cancellationToken);
    }
}
