using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces
{
    /// <summary>
    /// Orchestrates the execution of commands with dependency resolution and flexible ordering
    /// </summary>
    /// <typeparam name="TInput">The input type for commands</typeparam>
    /// <typeparam name="TOutput">The output type for commands</typeparam>
    public interface ICommandOrchestrator<TInput, TOutput>
    {
        /// <summary>
        /// Executes a collection of commands in the optimal order
        /// </summary>
        /// <param name="commands">The commands to execute</param>
        /// <param name="input">The initial input</param>
        /// <param name="options">Execution options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The result of the command orchestration</returns>
        Task<OrchestrationResult<TOutput>> ExecuteAsync(
            IEnumerable<ICommand<TInput, TOutput>> commands,
            TInput input,
            CommandExecutionOptions? options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes commands in a specific order
        /// </summary>
        /// <param name="commandIds">The IDs of commands to execute in order</param>
        /// <param name="availableCommands">The available commands</param>
        /// <param name="input">The initial input</param>
        /// <param name="options">Execution options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The result of the command orchestration</returns>
        Task<OrchestrationResult<TOutput>> ExecuteInOrderAsync(
            IEnumerable<string> commandIds,
            IEnumerable<ICommand<TInput, TOutput>> availableCommands,
            TInput input,
            CommandExecutionOptions? options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates that all commands can be executed with the given input
        /// </summary>
        /// <param name="commands">The commands to validate</param>
        /// <param name="input">The input to validate against</param>
        /// <returns>Validation result with any issues found</returns>
        CommandValidationResult Validate(IEnumerable<ICommand<TInput, TOutput>> commands, TInput input);

        /// <summary>
        /// Gets the execution plan for the given commands
        /// </summary>
        /// <param name="commands">The commands to plan</param>
        /// <returns>The execution plan showing command order and dependencies</returns>
        CommandExecutionPlan GetExecutionPlan(IEnumerable<ICommand<TInput, TOutput>> commands);
    }

    /// <summary>
    /// Options for command execution
    /// </summary>
    public class CommandExecutionOptions
    {
        /// <summary>
        /// Gets or sets whether to enable parallel execution where possible
        /// </summary>
        public bool EnableParallelExecution { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum degree of parallelism
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

        /// <summary>
        /// Gets or sets whether to continue execution after a command failure
        /// </summary>
        public bool ContinueOnFailure { get; set; } = false;

        /// <summary>
        /// Gets or sets the timeout for the entire orchestration
        /// </summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Gets or sets whether to validate commands before execution
        /// </summary>
        public bool ValidateBeforeExecution { get; set; } = true;
    }

    /// <summary>
    /// Result of command orchestration
    /// </summary>
    /// <typeparam name="T">The result type</typeparam>
    public class OrchestrationResult<T>
    {
        /// <summary>
        /// Gets or sets whether the orchestration was successful
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Gets or sets the final result data
        /// </summary>
        public T? FinalResult { get; set; }

        /// <summary>
        /// Gets or sets the results of individual command executions
        /// </summary>
        public Dictionary<string, CommandResult<object>> CommandResults { get; set; } = new();

        /// <summary>
        /// Gets or sets any errors that occurred during orchestration
        /// </summary>
        public string[] Errors { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the total execution time
        /// </summary>
        public TimeSpan TotalExecutionTime { get; set; }

        /// <summary>
        /// Gets or sets additional metadata about the orchestration
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Result of command validation
    /// </summary>
    public class CommandValidationResult
    {
        /// <summary>
        /// Gets or sets whether the validation passed
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets any validation errors
        /// </summary>
        public string[] Errors { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets any validation warnings
        /// </summary>
        public string[] Warnings { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Execution plan for commands
    /// </summary>
    public class CommandExecutionPlan
    {
        /// <summary>
        /// Gets or sets the commands in execution order
        /// </summary>
        public List<string> ExecutionOrder { get; set; } = new();

        /// <summary>
        /// Gets or sets the estimated execution time
        /// </summary>
        public TimeSpan EstimatedExecutionTime { get; set; }

        /// <summary>
        /// Gets or sets any dependencies between commands
        /// </summary>
        public Dictionary<string, List<string>> Dependencies { get; set; } = new();

        /// <summary>
        /// Gets or sets whether parallel execution is possible
        /// </summary>
        public bool CanExecuteInParallel { get; set; }
    }
}
