namespace Nexo.Agents.AutonomousDev.Models;

/// <summary>
/// AI's decision about what to do next after analyzing test feedback.
/// </summary>
public record IterationDecision
{
    public required DecisionType Decision { get; init; }
    public required string Reasoning { get; init; }
    
    /// <summary>
    /// If iterating, what specifically needs to change.
    /// </summary>
    public IReadOnlyList<PlannedFix> PlannedFixes { get; init; } = Array.Empty<PlannedFix>();
    
    /// <summary>
    /// Updated plan for remaining work.
    /// </summary>
    public DevelopmentPlan? UpdatedPlan { get; init; }
    
    /// <summary>
    /// Confidence in this decision (0-1).
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// Estimated iterations remaining.
    /// </summary>
    public int EstimatedRemainingIterations { get; init; }
    
    /// <summary>
    /// If aborting, why and what alternatives exist.
    /// </summary>
    public string? AbortReason { get; init; }
    public IReadOnlyList<string> AlternativeApproaches { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Type of decision made.
/// </summary>
public enum DecisionType
{
    /// <summary>
    /// All acceptance criteria met, ready to ship.
    /// </summary>
    Complete,
    
    /// <summary>
    /// Issues found, continue iterating.
    /// </summary>
    Iterate,
    
    /// <summary>
    /// Need human input before continuing.
    /// </summary>
    NeedsClarification,
    
    /// <summary>
    /// Stuck, can't make progress.
    /// </summary>
    Stuck,
    
    /// <summary>
    /// Too many iterations, giving up.
    /// </summary>
    MaxIterationsReached,
    
    /// <summary>
    /// Fundamental approach is wrong, need to restart.
    /// </summary>
    NeedsRedesign
}

/// <summary>
/// A planned fix for an issue.
/// </summary>
public record PlannedFix
{
    public required string IssueId { get; init; }
    public required string Description { get; init; }
    public required string ApproachDescription { get; init; }
    public IReadOnlyList<string> FilesToModify { get; init; } = Array.Empty<string>();
    public double ConfidenceInFix { get; init; }
}
