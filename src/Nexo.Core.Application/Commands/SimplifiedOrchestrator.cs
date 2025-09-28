using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Commands
{
    /// <summary>
    /// Simplified orchestrator following test structure principles:
    /// - Simple command composition
    /// - Clear execution flow
    /// - No complex dependency resolution
    /// - Direct success/failure handling
    /// </summary>
    public class SimplifiedOrchestrator<TInput, TOutput>
    {
        private readonly List<ISimplifiedCommand<object, object>> _commands = new();
        private readonly Dictionary<string, object> _context = new();

        /// <summary>
        /// Add a command to the orchestration (like adding a test)
        /// </summary>
        public void AddCommand<TCommandInput, TCommandOutput>(ISimplifiedCommand<TCommandInput, TCommandOutput> command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            _commands.Add((ISimplifiedCommand<object, object>)command);
        }

        /// <summary>
        /// Execute all commands in sequence (like running a test suite)
        /// </summary>
        public async Task<OrchestrationResult<TOutput>> ExecuteAsync(TInput input, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            var results = new List<CommandResult<object>>();
            var currentInput = (object)input;

            try
            {
                // Execute commands in sequence (like running tests one by one)
                foreach (var command in _commands)
                {
                    var result = await command.ExecuteAsync(currentInput, cancellationToken);
                    results.Add(result);

                    // Stop on first failure (like test failure stops the suite)
                    if (!result.IsSuccess)
                    {
                        return new OrchestrationResult<TOutput>
                        {
                            IsSuccess = false,
                            ErrorMessage = result.ErrorMessage,
                            ExecutionTime = DateTime.UtcNow - startTime,
                            CommandResults = results,
                            FinalResult = default(TOutput)
                        };
                    }

                    // Use the output as input for the next command (like test chaining)
                    if (result.Data != null)
                    {
                        currentInput = result.Data;
                    }
                }

                // Success - return the final result
                var finalResult = currentInput is TOutput output ? output : default(TOutput);
                return new OrchestrationResult<TOutput>
                {
                    IsSuccess = true,
                    ExecutionTime = DateTime.UtcNow - startTime,
                    CommandResults = results,
                    FinalResult = finalResult
                };
            }
            catch (Exception ex)
            {
                return new OrchestrationResult<TOutput>
                {
                    IsSuccess = false,
                    ErrorMessage = $"Orchestration failed: {ex.Message}",
                    ExecutionTime = DateTime.UtcNow - startTime,
                    CommandResults = results,
                    FinalResult = default(TOutput)
                };
            }
        }

        /// <summary>
        /// Get the number of commands in this orchestration
        /// </summary>
        public int CommandCount => _commands.Count;

        /// <summary>
        /// Clear all commands (like clearing test suite)
        /// </summary>
        public void Clear()
        {
            _commands.Clear();
            _context.Clear();
        }
    }

    /// <summary>
    /// Result of orchestration execution
    /// </summary>
    public class OrchestrationResult<TOutput>
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public List<CommandResult<object>> CommandResults { get; set; } = new();
        public TOutput? FinalResult { get; set; }
    }
}
