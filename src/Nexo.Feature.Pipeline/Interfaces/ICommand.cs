using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Nexo.Shared.Models;
using Nexo.Feature.Pipeline.Enums;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.Feature.Pipeline.Interfaces
{
/// <summary>
/// Generic command interface for typed commands
/// </summary>
public interface ICommand<in TRequest, TResponse>
{
    Task<TResponse> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Command attribute for marking command classes
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class CommandAttribute : Attribute
{
    public string Name { get; }
    
    public CommandAttribute(string name)
    {
        Name = name;
    }
}

/// <summary>
/// Behavior attribute for marking behavior classes
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class BehaviorAttribute : Attribute
{
    public string Name { get; }
    
    public BehaviorAttribute(string name)
    {
        Name = name;
    }
}

/// <summary>
/// Represents an atomic operation in the pipeline architecture.
/// Commands are the fundamental building blocks that can be composed into behaviors.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Unique identifier for this command.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Human-readable name for this command.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Description of what this command does.
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Category this command belongs to (e.g., FileSystem, Container, Analysis).
    /// </summary>
    CommandCategory Category { get; }
    
    /// <summary>
    /// Tags for flexible organization and discovery.
    /// </summary>
    IReadOnlyList<string> Tags { get; }
    
    /// <summary>
    /// Priority level for execution ordering.
    /// </summary>
    CommandPriority Priority { get; }
    
    /// <summary>
    /// Whether this command can be executed in parallel with other commands.
    /// </summary>
    bool CanExecuteInParallel { get; }
    
    /// <summary>
    /// Dependencies that must be satisfied before this command can execute.
    /// </summary>
    IReadOnlyList<CommandDependency> Dependencies { get; }
    
    /// <summary>
    /// Validates the command parameters and context before execution.
    /// </summary>
    /// <param name="context">The pipeline context.</param>
    /// <returns>Validation result indicating if the command can execute.</returns>
    Task<CommandValidationResult> ValidateAsync(IPipelineContext context);
    
    /// <summary>
    /// Executes the command with the given context.
    /// </summary>
    /// <param name="context">The pipeline context containing shared data and configuration.</param>
    /// <returns>Result of the command execution.</returns>
    Task<Nexo.Feature.Pipeline.Models.CommandResult> ExecuteAsync(IPipelineContext context);
    
    /// <summary>
    /// Performs cleanup operations after command execution.
    /// </summary>
    /// <param name="context">The pipeline context.</param>
    /// <returns>Task representing the cleanup operation.</returns>
    Task CleanupAsync(IPipelineContext context);
    
    /// <summary>
    /// Gets metadata about this command for discovery and documentation.
    /// </summary>
    /// <returns>Command metadata.</returns>
    CommandMetadata GetMetadata();
}
} 