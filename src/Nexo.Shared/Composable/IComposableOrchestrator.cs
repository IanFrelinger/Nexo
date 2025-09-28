using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Shared.Composable.CommandTypes;

namespace Nexo.Shared.Composable
{
    /// <summary>
    /// Interface for composable command orchestration
    /// </summary>
    public interface IComposableOrchestrator
    {
        /// <summary>
        /// Registers a command with the orchestrator
        /// </summary>
        Task<ICommandResult> RegisterCommandAsync(IComposableCommand command);
        
        /// <summary>
        /// Unregisters a command from the orchestrator
        /// </summary>
        Task<ICommandResult> UnregisterCommandAsync(ICommandIdentifier commandId);
        
        /// <summary>
        /// Executes a single command
        /// </summary>
        Task<ICommandResult> ExecuteCommandAsync(ICommandIdentifier commandId, ICommandContext context);
        
        /// <summary>
        /// Executes multiple commands in sequence
        /// </summary>
        Task<IReadOnlyList<ICommandResult>> ExecuteSequenceAsync(IReadOnlyList<ICommandIdentifier> commandIds, ICommandContext context);
        
        /// <summary>
        /// Executes multiple commands in parallel
        /// </summary>
        Task<IReadOnlyList<ICommandResult>> ExecuteParallelAsync(IReadOnlyList<ICommandIdentifier> commandIds, ICommandContext context);
        
        /// <summary>
        /// Executes commands based on a workflow definition
        /// </summary>
        Task<IWorkflowResult> ExecuteWorkflowAsync(IWorkflowDefinition workflow, ICommandContext context);
        
        /// <summary>
        /// Gets all registered commands
        /// </summary>
        Task<IReadOnlyList<IComposableCommand>> GetRegisteredCommandsAsync();
        
        /// <summary>
        /// Gets a specific command by ID
        /// </summary>
        Task<IComposableCommand> GetCommandAsync(ICommandIdentifier commandId);
        
        /// <summary>
        /// Validates a command execution plan
        /// </summary>
        Task<IValidationResult> ValidateExecutionPlanAsync(IReadOnlyList<ICommandIdentifier> commandIds, ICommandContext context);
    }
    
    /// <summary>
    /// Interface for workflow definitions
    /// </summary>
    public interface IWorkflowDefinition
    {
        string Name { get; }
        string Description { get; }
        IReadOnlyList<IWorkflowStep> Steps { get; }
        IReadOnlyDictionary<string, object> Parameters { get; }
    }
    
    /// <summary>
    /// Interface for workflow steps
    /// </summary>
    public interface IWorkflowStep
    {
        string Name { get; }
        ICommandIdentifier CommandId { get; }
        IReadOnlyList<ICommandIdentifier> Dependencies { get; }
        IReadOnlyDictionary<string, object> Parameters { get; }
        bool IsParallel { get; }
        bool IsOptional { get; }
    }
    
    /// <summary>
    /// Interface for workflow results
    /// </summary>
    public interface IWorkflowResult
    {
        bool IsSuccess { get; }
        string Message { get; }
        IReadOnlyList<ICommandResult> StepResults { get; }
        IReadOnlyDictionary<string, object> Metadata { get; }
        DateTime CompletedAt { get; }
        TimeSpan Duration { get; }
    }
}
