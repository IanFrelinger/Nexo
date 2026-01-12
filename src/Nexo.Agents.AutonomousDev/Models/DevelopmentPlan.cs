namespace Nexo.Agents.AutonomousDev.Models;

/// <summary>
/// Broken-down plan of how to implement the specification.
/// </summary>
public record DevelopmentPlan
{
    public required string SpecificationId { get; init; }
    
    /// <summary>
    /// Ordered list of tasks to complete.
    /// </summary>
    public IReadOnlyList<DevelopmentTask> Tasks { get; init; } = Array.Empty<DevelopmentTask>();
    
    /// <summary>
    /// How tasks depend on each other.
    /// </summary>
    public IReadOnlyList<TaskDependency> Dependencies { get; init; } = Array.Empty<TaskDependency>();
    
    /// <summary>
    /// Estimated total effort.
    /// </summary>
    public TimeSpan EstimatedDuration { get; init; }
    
    /// <summary>
    /// Files that will be created or modified.
    /// </summary>
    public IReadOnlyList<PlannedFileChange> PlannedChanges { get; init; } = Array.Empty<PlannedFileChange>();
    
    /// <summary>
    /// Testing strategy for this plan.
    /// </summary>
    public TestingStrategy TestStrategy { get; init; } = new();
}

/// <summary>
/// A single development task.
/// </summary>
public record DevelopmentTask
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required TaskType Type { get; init; }
    
    /// <summary>
    /// Detailed steps AI should take.
    /// </summary>
    public IReadOnlyList<string> Steps { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// Files to create or modify.
    /// </summary>
    public IReadOnlyList<string> TargetFiles { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// How to verify this task is complete.
    /// </summary>
    public string? VerificationMethod { get; init; }
    
    /// <summary>
    /// Current status.
    /// </summary>
    public TaskStatus Status { get; init; } = TaskStatus.Pending;
    
    /// <summary>
    /// Iteration attempts for this task.
    /// </summary>
    public int Attempts { get; init; }
}

/// <summary>
/// Type of development task.
/// </summary>
public enum TaskType
{
    CreateFile,
    ModifyFile,
    DeleteFile,
    CreateAsset,
    ModifyAsset,
    Configuration,
    Database,
    Test,
    Documentation
}

/// <summary>
/// Status of a task.
/// </summary>
public enum TaskStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Skipped
}

/// <summary>
/// Dependency between tasks.
/// </summary>
public record TaskDependency
{
    public required string TaskId { get; init; }
    public required string DependsOnTaskId { get; init; }
}

/// <summary>
/// A planned file change.
/// </summary>
public record PlannedFileChange
{
    public required string FilePath { get; init; }
    public required FileChangeType ChangeType { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// Type of file change.
/// </summary>
public enum FileChangeType
{
    Create,
    Modify,
    Delete,
    Rename
}

/// <summary>
/// Testing strategy for the development plan.
/// </summary>
public record TestingStrategy
{
    /// <summary>
    /// What to test after each iteration.
    /// </summary>
    public IReadOnlyList<string> TestCases { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// How the mock user should behave.
    /// </summary>
    public MockUserPersona Persona { get; init; }
    
    /// <summary>
    /// Specific flows to test.
    /// </summary>
    public IReadOnlyList<string> UserFlows { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// What constitutes passing.
    /// </summary>
    public double MinimumPassRate { get; init; } = 0.9;
}
