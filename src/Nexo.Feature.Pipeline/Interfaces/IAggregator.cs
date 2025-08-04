using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Nexo.Feature.Pipeline.Enums;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.Feature.Pipeline.Interfaces
{
    /// <summary>
    /// Represents the top-level orchestrator that manages the execution of behaviors and commands.
    /// Aggregators provide the highest level of abstraction and coordinate the overall pipeline execution.
    /// </summary>
    public interface IAggregator
    {
        /// <summary>
        /// Unique identifier for this aggregator.
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Human-readable name for this aggregator.
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Description of what this aggregator accomplishes.
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// Category this aggregator belongs to.
        /// </summary>
        AggregatorCategory Category { get; }
        
        /// <summary>
        /// Tags for flexible organization and discovery.
        /// </summary>
        IReadOnlyList<string> Tags { get; }
        
        /// <summary>
        /// Execution strategy for the behaviors within this aggregator.
        /// </summary>
        AggregatorExecutionStrategy ExecutionStrategy { get; }
        
        /// <summary>
        /// Behaviors that make up this aggregator.
        /// </summary>
        IReadOnlyList<IBehavior> Behaviors { get; }
        
        /// <summary>
        /// Commands that are executed directly by this aggregator (not through behaviors).
        /// </summary>
        IReadOnlyList<ICommand> DirectCommands { get; }
        
        /// <summary>
        /// Dependencies that must be satisfied before this aggregator can execute.
        /// </summary>
        IReadOnlyList<AggregatorDependency> Dependencies { get; }
        
        /// <summary>
        /// Resource requirements for this aggregator.
        /// </summary>
        ResourceRequirements ResourceRequirements { get; }
        
        /// <summary>
        /// Validates the aggregator, its behaviors, and commands before execution.
        /// </summary>
        /// <param name="context">The pipeline context.</param>
        /// <returns>Validation result indicating if the aggregator can execute.</returns>
        Task<AggregatorValidationResult> ValidateAsync(IPipelineContext context);
        
        /// <summary>
        /// Executes the aggregator by orchestrating its behaviors and commands.
        /// </summary>
        /// <param name="context">The pipeline context.</param>
        /// <returns>Result of the aggregator execution.</returns>
        Task<AggregatorResult> ExecuteAsync(IPipelineContext context);
        
        /// <summary>
        /// Performs cleanup operations after aggregator execution.
        /// </summary>
        /// <param name="context">The pipeline context.</param>
        /// <returns>Task representing the cleanup operation.</returns>
        Task CleanupAsync(IPipelineContext context);
        
        /// <summary>
        /// Gets metadata about this aggregator for discovery and documentation.
        /// </summary>
        /// <returns>Aggregator metadata.</returns>
        AggregatorMetadata GetMetadata();
        
        /// <summary>
        /// Adds a behavior to this aggregator.
        /// </summary>
        /// <param name="behavior">The behavior to add.</param>
        void AddBehavior(IBehavior behavior);
        
        /// <summary>
        /// Removes a behavior from this aggregator.
        /// </summary>
        /// <param name="behaviorId">The ID of the behavior to remove.</param>
        /// <returns>True if the behavior was found and removed, false otherwise.</returns>
        bool RemoveBehavior(string behaviorId);
        
        /// <summary>
        /// Adds a command directly to this aggregator.
        /// </summary>
        /// <param name="command">The command to add.</param>
        void AddDirectCommand(ICommand command);
        
        /// <summary>
        /// Removes a direct command from this aggregator.
        /// </summary>
        /// <param name="commandId">The ID of the command to remove.</param>
        /// <returns>True if the command was found and removed, false otherwise.</returns>
        bool RemoveDirectCommand(string commandId);
        
        /// <summary>
        /// Gets the execution plan for this aggregator.
        /// </summary>
        /// <param name="context">The pipeline context.</param>
        /// <returns>Execution plan showing how behaviors and commands will be executed.</returns>
        Task<AggregatorExecutionPlan> GetExecutionPlanAsync(IPipelineContext context);
        
        /// <summary>
        /// Gets performance metrics for this aggregator.
        /// </summary>
        /// <returns>Performance metrics.</returns>
        AggregatorPerformanceMetrics GetPerformanceMetrics();
    }
} 