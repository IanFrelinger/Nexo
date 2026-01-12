namespace Nexo.Agents.AutonomousDev.Models;

/// <summary>
/// Complete development session state.
/// </summary>
public record DevelopmentSession
{
    public required string Task { get; init; }
    public required string ProjectPath { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public SessionStatus Status { get; init; }
    
    public Specification? Specification { get; init; }
    public DevelopmentPlan? Plan { get; init; }
    public IReadOnlyList<IterationRecord> Iterations { get; init; } = Array.Empty<IterationRecord>();
}

/// <summary>
/// Status of the development session.
/// </summary>
public enum SessionStatus
{
    InProgress,
    Completed,
    Failed,
    Partial,
    MaxIterations,
    AwaitingInput
}

/// <summary>
/// Record of a single iteration.
/// </summary>
public record IterationRecord
{
    public int Number { get; init; }
    public required TestFeedback Feedback { get; init; }
    public IReadOnlyList<GeneratedArtifact> Artifacts { get; init; } = Array.Empty<GeneratedArtifact>();
}
