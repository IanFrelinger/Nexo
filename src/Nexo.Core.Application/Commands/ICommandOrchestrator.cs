using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Commands
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
        /// Gets or sets the maximum number of retry attempts for failed commands
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the delay between retry attempts
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets whether to enable undo on failure
        /// </summary>
        public bool EnableUndoOnFailure { get; set; } = true;

        /// <summary>
        /// Gets or sets the timeout for the entire command execution
        /// </summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Gets or sets whether to run in dry-run mode (validation only)
        /// </summary>
        public bool DryRun { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to stop on first failure
        /// </summary>
        public bool StopOnFirstFailure { get; set; } = true;
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
        public Dictionary<string, CommandResult<T>> CommandResults { get; set; } = new Dictionary<string, CommandResult<T>>();

        /// <summary>
        /// Gets or sets any errors that occurred during execution
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets any warnings generated during execution
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the total execution time
        /// </summary>
        public TimeSpan TotalDuration { get; set; }

        /// <summary>
        /// Gets or sets the execution plan that was followed
        /// </summary>
        public CommandExecutionPlan? ExecutionPlan { get; set; }

        /// <summary>
        /// Gets or sets additional metadata about the execution
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of command validation
    /// </summary>
    public class CommandValidationResult
    {
        /// <summary>
        /// Gets or sets whether the command set is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets any validation errors
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets any validation warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the execution plan that would be used
        /// </summary>
        public CommandExecutionPlan? ExecutionPlan { get; set; }
    }

    /// <summary>
    /// Represents the execution plan for a set of commands
    /// </summary>
    public class CommandExecutionPlan
    {
        /// <summary>
        /// Gets or sets the execution phases
        /// </summary>
        public List<CommandExecutionPhase> Phases { get; set; } = new List<CommandExecutionPhase>();

        /// <summary>
        /// Gets or sets the estimated total execution time
        /// </summary>
        public TimeSpan EstimatedDuration { get; set; }

        /// <summary>
        /// Gets or sets whether the plan includes parallel execution
        /// </summary>
        public bool HasParallelExecution { get; set; }

        /// <summary>
        /// Gets or sets the total number of commands
        /// </summary>
        public int TotalCommands { get; set; }
    }

    /// <summary>
    /// Represents a phase in the command execution plan
    /// </summary>
    public class CommandExecutionPhase
    {
        /// <summary>
        /// Gets or sets the phase number
        /// </summary>
        public int PhaseNumber { get; set; }

        /// <summary>
        /// Gets or sets the command IDs in this phase
        /// </summary>
        public List<string> CommandIds { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets whether commands in this phase can run in parallel
        /// </summary>
        public bool CanRunInParallel { get; set; }

        /// <summary>
        /// Gets or sets the estimated duration for this phase
        /// </summary>
        public TimeSpan EstimatedDuration { get; set; }
    }
}
