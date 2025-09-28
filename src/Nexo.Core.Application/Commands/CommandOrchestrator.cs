using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces;

namespace Nexo.Core.Application.Commands
{
    /// <summary>
    /// Simple command orchestrator that executes commands in sequence
    /// </summary>
    /// <typeparam name="TInput">The input type for commands</typeparam>
    /// <typeparam name="TOutput">The output type for commands</typeparam>
    public class CommandOrchestrator<TInput, TOutput> : ICommandOrchestrator<TInput, TOutput>
    {
        /// <summary>
        /// Executes a collection of commands in sequence
        /// </summary>
        public async Task<OrchestrationResult<TOutput>> ExecuteAsync(
            IEnumerable<ICommand<TInput, TOutput>> commands,
            TInput input,
            CommandExecutionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var commandList = commands.ToList();
            var startTime = DateTime.UtcNow;
            var results = new Dictionary<string, CommandResult<object>>();
            var errors = new List<string>();

            // Validate commands if requested
            if (options?.ValidateBeforeExecution == true)
            {
                var validationResult = Validate(commandList, input);
                if (!validationResult.IsValid)
                {
                    return new OrchestrationResult<TOutput>
                    {
                        IsSuccess = false,
                        Errors = validationResult.Errors,
                        TotalExecutionTime = DateTime.UtcNow - startTime
                    };
                }
            }

            TOutput? finalResult = default;
            var currentInput = input;

            foreach (var command in commandList)
            {
                try
                {
                    var result = await command.ExecuteAsync(currentInput, cancellationToken);
                    results[command.Id] = new CommandResult<object>
                    {
                        IsSuccess = result.IsSuccess,
                        Data = result.Data,
                        ErrorMessage = result.ErrorMessage,
                        ExecutionTime = result.ExecutionTime,
                        Warnings = result.Warnings,
                        Metadata = result.Metadata
                    };

                    if (!result.IsSuccess)
                    {
                        errors.Add($"Command '{command.Name}' failed: {result.ErrorMessage}");
                        if (!options?.ContinueOnFailure == true)
                        {
                            break;
                        }
                    }
                    else
                    {
                        finalResult = result.Data;
                        // For simplicity, pass the result as the next input
                        // In a real implementation, you'd have more sophisticated data flow
                        if (result.Data is TInput nextInput)
                        {
                            currentInput = nextInput;
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Command '{command.Name}' threw an exception: {ex.Message}");
                    if (!options?.ContinueOnFailure == true)
                    {
                        break;
                    }
                }
            }

            var totalTime = DateTime.UtcNow - startTime;
            return new OrchestrationResult<TOutput>
            {
                IsSuccess = errors.Count == 0,
                FinalResult = finalResult,
                CommandResults = results,
                Errors = errors.ToArray(),
                TotalExecutionTime = totalTime
            };
        }

        /// <summary>
        /// Executes commands in a specific order
        /// </summary>
        public async Task<OrchestrationResult<TOutput>> ExecuteInOrderAsync(
            IEnumerable<string> commandIds,
            IEnumerable<ICommand<TInput, TOutput>> availableCommands,
            TInput input,
            CommandExecutionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var commandMap = availableCommands.ToDictionary(c => c.Id, c => c);
            var orderedCommands = commandIds
                .Where(id => commandMap.ContainsKey(id))
                .Select(id => commandMap[id])
                .ToList();

            return await ExecuteAsync(orderedCommands, input, options, cancellationToken);
        }

        /// <summary>
        /// Validates that all commands can be executed with the given input
        /// </summary>
        public CommandValidationResult Validate(IEnumerable<ICommand<TInput, TOutput>> commands, TInput input)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            foreach (var command in commands)
            {
                try
                {
                    // Basic validation - in a real implementation, you'd have more sophisticated validation
                    if (string.IsNullOrEmpty(command.Id))
                    {
                        errors.Add($"Command has empty ID");
                    }
                    if (string.IsNullOrEmpty(command.Name))
                    {
                        errors.Add($"Command '{command.Id}' has empty name");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Error validating command '{command.Id}': {ex.Message}");
                }
            }

            return new CommandValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors.ToArray(),
                Warnings = warnings.ToArray()
            };
        }

        /// <summary>
        /// Gets the execution plan for the given commands
        /// </summary>
        public CommandExecutionPlan GetExecutionPlan(IEnumerable<ICommand<TInput, TOutput>> commands)
        {
            var commandList = commands.ToList();
            var totalEstimatedTime = TimeSpan.FromSeconds(commandList.Count * 1.0); // Simple estimation

            return new CommandExecutionPlan
            {
                ExecutionOrder = commandList.Select(c => c.Id).ToList(),
                EstimatedExecutionTime = totalEstimatedTime,
                CanExecuteInParallel = false // Simple implementation executes sequentially
            };
        }
    }
}
