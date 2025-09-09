using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Nexo.Feature.Pipeline.Enums;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.Feature.Pipeline.Interfaces
{
/// <summary>
/// Pipeline behavior interface for MediatR-style pipeline behaviors
/// </summary>
public interface IPipelineBehavior<in TRequest, TResponse>
{
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}

/// <summary>
/// Pipeline behavior interface for MediatR-style pipeline behaviors
/// </summary>
public interface IPipelineBehavior
{
    Task<TResponse> Handle<TRequest, TResponse>(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request handler delegate
/// </summary>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
/// Represents a composition of commands that work together to achieve a specific goal.
/// Behaviors provide higher-level abstractions and can be composed into aggregators.
/// </summary>
public interface IBehavior
{
    /// <summary>
    /// Unique identifier for this behavior.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Human-readable name for this behavior.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Description of what this behavior accomplishes.
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Category this behavior belongs to.
    /// </summary>
    BehaviorCategory Category { get; }
    
    /// <summary>
    /// Tags for flexible organization and discovery.
    /// </summary>
    IReadOnlyList<string> Tags { get; }
    
    /// <summary>
    /// Execution strategy for the commands within this behavior.
    /// </summary>
    BehaviorExecutionStrategy ExecutionStrategy { get; }
    
    /// <summary>
    /// Commands that make up this behavior.
    /// </summary>
    IReadOnlyList<ICommand> Commands { get; }
    
    /// <summary>
    /// Dependencies that must be satisfied before this behavior can execute.
    /// </summary>
    IReadOnlyList<BehaviorDependency> Dependencies { get; }
    
    /// <summary>
    /// Whether this behavior can be executed in parallel with other behaviors.
    /// </summary>
    bool CanExecuteInParallel { get; }
    
    /// <summary>
    /// Validates the behavior and its commands before execution.
    /// </summary>
    /// <param name="context">The pipeline context.</param>
    /// <returns>Validation result indicating if the behavior can execute.</returns>
    Task<BehaviorValidationResult> ValidateAsync(IPipelineContext context);
    
    /// <summary>
    /// Executes the behavior by orchestrating its commands according to the execution strategy.
    /// </summary>
    /// <param name="context">The pipeline context.</param>
    /// <returns>Result of the behavior execution.</returns>
    Task<BehaviorResult> ExecuteAsync(IPipelineContext context);
    
    /// <summary>
    /// Performs cleanup operations after behavior execution.
    /// </summary>
    /// <param name="context">The pipeline context.</param>
    /// <returns>Task representing the cleanup operation.</returns>
    Task CleanupAsync(IPipelineContext context);
    
    /// <summary>
    /// Gets metadata about this behavior for discovery and documentation.
    /// </summary>
    /// <returns>Behavior metadata.</returns>
    BehaviorMetadata GetMetadata();
    
    /// <summary>
    /// Adds a command to this behavior.
    /// </summary>
    /// <param name="command">The command to add.</param>
    void AddCommand(ICommand command);
    
    /// <summary>
    /// Removes a command from this behavior.
    /// </summary>
    /// <param name="commandId">The ID of the command to remove.</param>
    /// <returns>True if the command was found and removed, false otherwise.</returns>
    bool RemoveCommand(string commandId);
    
    /// <summary>
    /// Gets the execution plan for this behavior.
    /// </summary>
    /// <param name="context">The pipeline context.</param>
    /// <returns>Execution plan showing how commands will be executed.</returns>
    Task<BehaviorExecutionPlan> GetExecutionPlanAsync(IPipelineContext context);
}
} 