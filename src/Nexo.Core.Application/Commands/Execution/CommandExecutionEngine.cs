using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Commands.Execution
{
    /// <summary>
    /// Engine responsible for executing commands in sequence or parallel
    /// </summary>
    /// <typeparam name="TInput">The input type for commands</typeparam>
    /// <typeparam name="TOutput">The output type for commands</typeparam>
    public class CommandExecutionEngine<TInput, TOutput>
    {
        /// <summary>
        /// Executes commands in sequence
        /// </summary>
        public async Task<OrchestrationResult<TOutput>> ExecuteInSequenceAsync(
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
                var commandResults = new Dictionary<string, CommandResult<TOutput>>();
                var errors = new List<string>();
                var warnings = new List<string>();
                var currentInput = input;
                TOutput? finalResult = default;

                foreach (var command in commandList)
                {
                    try
                    {
                        var result = await command.ExecuteAsync(currentInput, cancellationToken);
                        commandResults[command.CommandId] = result;

                        if (result.IsSuccess)
                        {
                            if (result.Data != null)
                            {
                                finalResult = result.Data;
                                currentInput = input; // Reset input for next command
                            }
                        }
                        else
                        {
                            errors.Add($"Command {command.CommandName} failed: {result.ErrorMessage}");
                            if (!options_.ContinueOnFailure)
                            {
                                break;
                            }
                        }

                        if (result.Warnings?.Any() == true)
                        {
                            warnings.AddRange(result.Warnings);
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Command {command.CommandName} threw exception: {ex.Message}");
                        if (!options_.ContinueOnFailure)
                        {
                            break;
                        }
                    }
                }

                return new OrchestrationResult<TOutput>
                {
                    IsSuccess = errors.Count == 0,
                    FinalResult = finalResult ?? default(TOutput)!,
                    CommandResults = commandResults,
                    Errors = errors,
                    Warnings = warnings,
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

        /// <summary>
        /// Executes commands in parallel
        /// </summary>
        public async Task<OrchestrationResult<TOutput>> ExecuteInParallelAsync(
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
                var tasks = commandList.Select(async command =>
                {
                    try
                    {
                        return new { Command = command, Result = await command.ExecuteAsync(input, cancellationToken) };
                    }
                    catch (Exception ex)
                    {
                        return new { Command = command, Result = new CommandResult<TOutput> { IsSuccess = false, ErrorMessage = ex.Message } };
                    }
                });

                var results = await Task.WhenAll(tasks);
                var commandResults = results.ToDictionary(r => r.Command.CommandId, r => r.Result);
                var errors = results.Where(r => !r.Result.IsSuccess).Select(r => $"Command {r.Command.CommandName} failed: {r.Result.ErrorMessage}").ToList();
                var warnings = results.SelectMany(r => r.Result.Warnings ?? Array.Empty<string>()).ToList();

                return new OrchestrationResult<TOutput>
                {
                    IsSuccess = errors.Count == 0,
                    FinalResult = results.FirstOrDefault(r => r.Result.IsSuccess)?.Result.Data ?? default(TOutput)!,
                    CommandResults = commandResults,
                    Errors = errors,
                    Warnings = warnings,
                    TotalDuration = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new OrchestrationResult<TOutput>
                {
                    IsSuccess = false,
                    Errors = new List<string> { $"Parallel orchestration failed: {ex.Message}" },
                    TotalDuration = DateTime.UtcNow - startTime
                };
            }
        }
    }
}
