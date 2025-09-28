using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Commands
{
    /// <summary>
    /// Concrete implementation of command orchestrator that handles dependency resolution and execution
    /// </summary>
    /// <typeparam name="TInput">The input type for commands</typeparam>
    /// <typeparam name="TOutput">The output type for commands</typeparam>
    public class CommandOrchestrator<TInput, TOutput> : ICommandOrchestrator<TInput, TOutput>
    {
        public async Task<OrchestrationResult<TOutput>> ExecuteAsync(
            IEnumerable<ICommand<TInput, TOutput>> commands,
            TInput input,
            CommandExecutionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            var commandList = commands.ToList();
            var options_ = options ?? new CommandExecutionOptions();

            try
            {
                // Validate all commands first
                var validationResult = Validate(commands, input);
                if (!validationResult.IsValid && !options_.ContinueOnFailure)
                {
                    return new OrchestrationResult<TOutput>
                    {
                        IsSuccess = false,
                        Errors = validationResult.Errors,
                        Warnings = validationResult.Warnings,
                        ExecutionPlan = validationResult.ExecutionPlan,
                        TotalDuration = DateTime.UtcNow - startTime
                    };
                }

                // Get execution plan
                var executionPlan = GetExecutionPlan(commands);
                var commandResults = new Dictionary<string, CommandResult<TOutput>>();
                var errors = new List<string>();
                var warnings = new List<string>();
                var currentInput = input;
                TOutput? finalResult = default;

                // Execute commands according to the plan
                foreach (var phase in executionPlan.Phases)
                {
                    if (options_.EnableParallelExecution && phase.CanRunInParallel)
                    {
                        // Execute commands in parallel
                        var parallelTasks = phase.CommandIds.Select(async commandId =>
                        {
                            var command = commandList.First(c => c.CommandId == commandId);
                            var result = await ExecuteCommandWithRetry(command, currentInput, options_, cancellationToken);
                            return new { CommandId = commandId, Result = result };
                        });

                        var parallelResults = await Task.WhenAll(parallelTasks);
                        
                        foreach (var result in parallelResults)
                        {
                            commandResults[result.CommandId] = result.Result;
                            if (!result.Result.IsSuccess)
                            {
                                errors.Add($"Command {result.CommandId} failed: {result.Result.ErrorMessage}");
                                if (options_.StopOnFirstFailure)
                                {
                                    break;
                                }
                            }
                            warnings.AddRange(result.Result.Warnings);
                        }
                    }
                    else
                    {
                        // Execute commands sequentially
                        foreach (var commandId in phase.CommandIds)
                        {
                            var command = commandList.First(c => c.CommandId == commandId);
                            var result = await ExecuteCommandWithRetry(command, currentInput, options_, cancellationToken);
                            
                            commandResults[commandId] = result;
                            
                            if (!result.IsSuccess)
                            {
                                errors.Add($"Command {commandId} failed: {result.ErrorMessage}");
                                if (options_.StopOnFirstFailure)
                                {
                                    break;
                                }
                            }
                            else
                            {
                                // Update input for next command (if the command produces output)
                                if (result.Data != null)
                                {
                                    // This is a simplified approach - in practice, you'd need proper type handling
                                    // currentInput = TransformInput(currentInput, result.Data);
                                }
                            }
                            
                            warnings.AddRange(result.Warnings);
                        }
                    }

                    if (errors.Any() && options_.StopOnFirstFailure)
                    {
                        break;
                    }
                }

                // Determine final result
                var isSuccess = !errors.Any() || options_.ContinueOnFailure;
                if (commandResults.Values.Any(r => r.IsSuccess && r.Data != null))
                {
                    finalResult = commandResults.Values.Last(r => r.IsSuccess && r.Data != null).Data;
                }

                return new OrchestrationResult<TOutput>
                {
                    IsSuccess = isSuccess,
                    FinalResult = finalResult,
                    CommandResults = commandResults,
                    Errors = errors,
                    Warnings = warnings,
                    ExecutionPlan = executionPlan,
                    TotalDuration = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new OrchestrationResult<TOutput>
                {
                    IsSuccess = false,
                    Errors = new List<string> { $"Orchestration failed: {ex.Message}" },
                    TotalDuration = DateTime.UtcNow - startTime
                };
            }
        }

        public async Task<OrchestrationResult<TOutput>> ExecuteInOrderAsync(
            IEnumerable<string> commandIds,
            IEnumerable<ICommand<TInput, TOutput>> availableCommands,
            TInput input,
            CommandExecutionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var commandList = availableCommands.ToList();
            var commandsToExecute = commandIds
                .Select(id => commandList.FirstOrDefault(c => c.CommandId == id))
                .Where(c => c != null)
                .Cast<ICommand<TInput, TOutput>>();

            return await ExecuteAsync(commandsToExecute, input, options, cancellationToken);
        }

        public CommandValidationResult Validate(IEnumerable<ICommand<TInput, TOutput>> commands, TInput input)
        {
            var commandList = commands.ToList();
            var errors = new List<string>();
            var warnings = new List<string>();

            // Check for duplicate command IDs
            var duplicateIds = commandList
                .GroupBy(c => c.CommandId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            if (duplicateIds.Any())
            {
                errors.Add($"Duplicate command IDs found: {string.Join(", ", duplicateIds)}");
            }

            // Check dependencies
            var commandIds = commandList.Select(c => c.CommandId).ToHashSet();
            foreach (var command in commandList)
            {
                var missingDependencies = command.Dependencies
                    .Where(dep => !commandIds.Contains(dep))
                    .ToList();

                if (missingDependencies.Any())
                {
                    errors.Add($"Command {command.CommandId} has missing dependencies: {string.Join(", ", missingDependencies)}");
                }
            }

            // Check for circular dependencies
            var circularDeps = FindCircularDependencies(commandList);
            if (circularDeps.Any())
            {
                errors.Add($"Circular dependencies found: {string.Join(", ", circularDeps)}");
            }

            // Validate individual commands
            foreach (var command in commandList)
            {
                if (!command.CanExecute(input))
                {
                    warnings.Add($"Command {command.CommandId} cannot be executed with current input");
                }
            }

            var executionPlan = GetExecutionPlan(commands);

            return new CommandValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors,
                Warnings = warnings,
                ExecutionPlan = executionPlan
            };
        }

        public CommandExecutionPlan GetExecutionPlan(IEnumerable<ICommand<TInput, TOutput>> commands)
        {
            var commandList = commands.ToList();
            var phases = new List<CommandExecutionPhase>();
            var processedCommands = new HashSet<string>();
            var currentPhase = 0;

            while (processedCommands.Count < commandList.Count)
            {
                var currentPhaseCommands = new List<string>();
                
                // Find commands that can be executed in this phase
                foreach (var command in commandList)
                {
                    if (processedCommands.Contains(command.CommandId))
                        continue;

                    // Check if all dependencies are satisfied
                    var dependenciesSatisfied = command.Dependencies.All(dep => processedCommands.Contains(dep));
                    
                    if (dependenciesSatisfied)
                    {
                        currentPhaseCommands.Add(command.CommandId);
                    }
                }

                if (currentPhaseCommands.Count == 0)
                {
                    // This shouldn't happen if dependencies are valid, but handle gracefully
                    break;
                }

                // Determine if commands in this phase can run in parallel
                var canRunInParallel = currentPhaseCommands.All(id =>
                {
                    var command = commandList.First(c => c.CommandId == id);
                    return command.CanExecuteInParallel;
                });

                phases.Add(new CommandExecutionPhase
                {
                    PhaseNumber = currentPhase,
                    CommandIds = currentPhaseCommands,
                    CanRunInParallel = canRunInParallel,
                    EstimatedDuration = TimeSpan.FromSeconds(currentPhaseCommands.Count * 1.0) // Simplified estimation
                });

                foreach (var commandId in currentPhaseCommands)
                {
                    processedCommands.Add(commandId);
                }

                currentPhase++;
            }

            return new CommandExecutionPlan
            {
                Phases = phases,
                EstimatedDuration = TimeSpan.FromSeconds(phases.Sum(p => p.EstimatedDuration.TotalSeconds)),
                HasParallelExecution = phases.Any(p => p.CanRunInParallel),
                TotalCommands = commandList.Count
            };
        }

        private async Task<CommandResult<TOutput>> ExecuteCommandWithRetry(
            ICommand<TInput, TOutput> command,
            TInput input,
            CommandExecutionOptions options,
            CancellationToken cancellationToken)
        {
            var attempt = 0;
            var maxAttempts = options.MaxRetryAttempts + 1;

            while (attempt < maxAttempts)
            {
                try
                {
                    var result = await command.ExecuteAsync(input, cancellationToken);
                    
                    if (result.IsSuccess || attempt == maxAttempts - 1)
                    {
                        return result;
                    }

                    // Wait before retry
                    if (attempt < maxAttempts - 1)
                    {
                        await Task.Delay(options.RetryDelay, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    if (attempt == maxAttempts - 1)
                    {
                        return CommandResult<TOutput>.Failure(
                            $"Command {command.CommandId} failed after {maxAttempts} attempts: {ex.Message}",
                            TimeSpan.Zero
                        );
                    }
                }

                attempt++;
            }

            return CommandResult<TOutput>.Failure(
                $"Command {command.CommandId} failed after {maxAttempts} attempts",
                TimeSpan.Zero
            );
        }

        private List<string> FindCircularDependencies(List<ICommand<TInput, TOutput>> commands)
        {
            var circularDeps = new List<string>();
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();

            foreach (var command in commands)
            {
                if (!visited.Contains(command.CommandId))
                {
                    if (HasCircularDependency(command.CommandId, commands, visited, recursionStack))
                    {
                        circularDeps.Add(command.CommandId);
                    }
                }
            }

            return circularDeps;
        }

        private bool HasCircularDependency(
            string commandId,
            List<ICommand<TInput, TOutput>> commands,
            HashSet<string> visited,
            HashSet<string> recursionStack)
        {
            visited.Add(commandId);
            recursionStack.Add(commandId);

            var command = commands.FirstOrDefault(c => c.CommandId == commandId);
            if (command != null)
            {
                foreach (var dependency in command.Dependencies)
                {
                    if (!visited.Contains(dependency))
                    {
                        if (HasCircularDependency(dependency, commands, visited, recursionStack))
                        {
                            return true;
                        }
                    }
                    else if (recursionStack.Contains(dependency))
                    {
                        return true;
                    }
                }
            }

            recursionStack.Remove(commandId);
            return false;
        }
    }
}
