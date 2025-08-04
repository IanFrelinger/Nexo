using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Feature.Pipeline.Enums;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Represents an execution plan for an aggregator.
    /// </summary>
    public class AggregatorExecutionPlan
{
    /// <summary>
    /// The aggregator this plan is for.
    /// </summary>
    public string AggregatorId { get; set; } = string.Empty;
    
    /// <summary>
    /// The aggregator name.
    /// </summary>
    public string AggregatorName { get; set; } = string.Empty;
    
    /// <summary>
    /// Execution strategy for this aggregator.
    /// </summary>
    public AggregatorExecutionStrategy ExecutionStrategy { get; set; }
    
    /// <summary>
    /// Phases of behavior and command execution.
    /// </summary>
    public List<AggregatorExecutionPhase> Phases { get; set; } = new List<AggregatorExecutionPhase>();
    
    /// <summary>
    /// Estimated execution time in milliseconds.
    /// </summary>
    public long EstimatedExecutionTimeMs { get; set; }
    
    /// <summary>
    /// Resource requirements for this execution plan.
    /// </summary>
    public ResourceRequirements ResourceRequirements { get; set; } = new ResourceRequirements();
    
    /// <summary>
    /// Dependencies that must be satisfied before execution.
    /// </summary>
    public List<AggregatorDependency> Dependencies { get; set; } = new List<AggregatorDependency>();
    
    /// <summary>
    /// Additional metadata about the execution plan.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// Represents a phase of aggregator execution.
/// </summary>
public class AggregatorExecutionPhase
{
    /// <summary>
    /// Phase number (order of execution).
    /// </summary>
    public int PhaseNumber { get; set; }
    
    /// <summary>
    /// Name of the phase.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of what this phase does.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Behaviors in this phase.
    /// </summary>
    public List<PlannedBehavior> Behaviors { get; set; } = new List<PlannedBehavior>();
    
    /// <summary>
    /// Direct commands in this phase.
    /// </summary>
    public List<PlannedCommand> DirectCommands { get; set; } = new List<PlannedCommand>();
    
    /// <summary>
    /// Whether items in this phase can be executed in parallel.
    /// </summary>
    public bool CanExecuteInParallel { get; set; }
    
    /// <summary>
    /// Estimated execution time for this phase in milliseconds.
    /// </summary>
    public long EstimatedExecutionTimeMs { get; set; }
}

/// <summary>
/// Represents a behavior in an aggregator execution plan.
/// </summary>
public class PlannedBehavior
{
    /// <summary>
    /// Behavior ID.
    /// </summary>
    public string BehaviorId { get; set; } = string.Empty;
    
    /// <summary>
    /// Behavior name.
    /// </summary>
    public string BehaviorName { get; set; } = string.Empty;
    
    /// <summary>
    /// Behavior category.
    /// </summary>
    public BehaviorCategory Category { get; set; }
    
    /// <summary>
    /// Execution strategy for this behavior.
    /// </summary>
    public BehaviorExecutionStrategy ExecutionStrategy { get; set; }
    
    /// <summary>
    /// Whether this behavior can be executed in parallel with other behaviors.
    /// </summary>
    public bool CanExecuteInParallel { get; set; }
    
    /// <summary>
    /// Dependencies for this behavior.
    /// </summary>
    public List<BehaviorDependency> Dependencies { get; set; } = new List<BehaviorDependency>();
    
    /// <summary>
    /// Estimated execution time in milliseconds.
    /// </summary>
    public long EstimatedExecutionTimeMs { get; set; }
}
}