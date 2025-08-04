using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Feature.Pipeline.Enums;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Represents an execution plan for a behavior.
    /// </summary>
    public class BehaviorExecutionPlan
{
    /// <summary>
    /// The behavior this plan is for.
    /// </summary>
    public string BehaviorId { get; set; } = string.Empty;
    
    /// <summary>
    /// The behavior name.
    /// </summary>
    public string BehaviorName { get; set; } = string.Empty;
    
    /// <summary>
    /// Execution strategy for this behavior.
    /// </summary>
    public BehaviorExecutionStrategy ExecutionStrategy { get; set; }
    
    /// <summary>
    /// Phases of command execution.
    /// </summary>
    public List<ExecutionPhase> Phases { get; set; } = new List<ExecutionPhase>();
    
    /// <summary>
    /// Estimated execution time in milliseconds.
    /// </summary>
    public long EstimatedExecutionTimeMs { get; set; }
    
    /// <summary>
    /// Whether this plan can be executed in parallel with other behaviors.
    /// </summary>
    public bool CanExecuteInParallel { get; set; }
    
    /// <summary>
    /// Dependencies that must be satisfied before execution.
    /// </summary>
    public List<BehaviorDependency> Dependencies { get; set; } = new List<BehaviorDependency>();
    
    /// <summary>
    /// Additional metadata about the execution plan.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// Represents a phase of command execution.
/// </summary>
public class ExecutionPhase
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
    /// Commands in this phase.
    /// </summary>
    public List<PlannedCommand> Commands { get; set; } = new List<PlannedCommand>();
    
    /// <summary>
    /// Whether commands in this phase can be executed in parallel.
    /// </summary>
    public bool CanExecuteInParallel { get; set; }
    
    /// <summary>
    /// Estimated execution time for this phase in milliseconds.
    /// </summary>
    public long EstimatedExecutionTimeMs { get; set; }
}

/// <summary>
/// Represents a command in an execution plan.
/// </summary>
public class PlannedCommand
{
    /// <summary>
    /// Command ID.
    /// </summary>
    public string CommandId { get; set; } = string.Empty;
    
    /// <summary>
    /// Command name.
    /// </summary>
    public string CommandName { get; set; } = string.Empty;
    
    /// <summary>
    /// Command category.
    /// </summary>
    public CommandCategory Category { get; set; }
    
    /// <summary>
    /// Command priority.
    /// </summary>
    public CommandPriority Priority { get; set; }
    
    /// <summary>
    /// Whether this command can be executed in parallel with other commands.
    /// </summary>
    public bool CanExecuteInParallel { get; set; }
    
    /// <summary>
    /// Dependencies for this command.
    /// </summary>
    public List<CommandDependency> Dependencies { get; set; } = new List<CommandDependency>();
    
    /// <summary>
    /// Estimated execution time in milliseconds.
    /// </summary>
    public long EstimatedExecutionTimeMs { get; set; }
}
}