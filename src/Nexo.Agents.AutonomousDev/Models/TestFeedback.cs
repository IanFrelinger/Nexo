using Nexo.Agents.UniversalTester.Models;

namespace Nexo.Agents.AutonomousDev.Models;

/// <summary>
/// Feedback from the Universal Testing Agent acting as a mock user.
/// </summary>
public record TestFeedback
{
    /// <summary>
    /// Which iteration produced this feedback.
    /// </summary>
    public int IterationNumber { get; init; }
    
    /// <summary>
    /// Did the feature work as expected?
    /// </summary>
    public bool OverallSuccess { get; init; }
    
    /// <summary>
    /// Score 0-100 based on how well acceptance criteria were met.
    /// </summary>
    public double AcceptanceScore { get; init; }
    
    /// <summary>
    /// Results for each acceptance criterion.
    /// </summary>
    public IReadOnlyList<CriterionResult> CriteriaResults { get; init; } = Array.Empty<CriterionResult>();
    
    /// <summary>
    /// What the mock user experienced.
    /// </summary>
    public required MockUserExperience UserExperience { get; init; }
    
    /// <summary>
    /// Issues discovered during testing.
    /// </summary>
    public IReadOnlyList<DiscoveredIssue> Issues { get; init; } = Array.Empty<DiscoveredIssue>();
    
    /// <summary>
    /// What worked well.
    /// </summary>
    public IReadOnlyList<string> Positives { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// Specific, actionable feedback for the next iteration.
    /// </summary>
    public IReadOnlyList<ActionableFeedback> ActionableItems { get; init; } = Array.Empty<ActionableFeedback>();
    
    /// <summary>
    /// Screenshots, logs, and other evidence.
    /// </summary>
    public IReadOnlyList<TestEvidence> Evidence { get; init; } = Array.Empty<TestEvidence>();
    
    /// <summary>
    /// Raw test report from Universal Tester.
    /// </summary>
    public TestReport? RawTestReport { get; init; }
}

/// <summary>
/// Result for a specific acceptance criterion.
/// </summary>
public record CriterionResult
{
    public required string CriterionId { get; init; }
    public required string Description { get; init; }
    public bool Passed { get; init; }
    public required string Observation { get; init; }
    public byte[]? Screenshot { get; init; }
}

/// <summary>
/// Mock user's experience during testing.
/// </summary>
public record MockUserExperience
{
    /// <summary>
    /// Natural language description of what the user experienced.
    /// </summary>
    public required string Narrative { get; init; }
    
    /// <summary>
    /// How intuitive was the feature? (1-10)
    /// </summary>
    public int IntuitivenessScore { get; init; }
    
    /// <summary>
    /// Did the user get confused at any point?
    /// </summary>
    public IReadOnlyList<ConfusionPoint> ConfusionPoints { get; init; } = Array.Empty<ConfusionPoint>();
    
    /// <summary>
    /// Did the user get frustrated?
    /// </summary>
    public bool UserFrustrated { get; init; }
    
    /// <summary>
    /// Time taken to complete the flow.
    /// </summary>
    public TimeSpan TimeToComplete { get; init; }
    
    /// <summary>
    /// How many attempts/retries were needed?
    /// </summary>
    public int AttemptCount { get; init; }
    
    /// <summary>
    /// Mock user's overall reaction.
    /// </summary>
    public required string OverallReaction { get; init; }
}

/// <summary>
/// A point where the user was confused.
/// </summary>
public record ConfusionPoint
{
    public required string Description { get; init; }
    public required string Location { get; init; }
    public byte[]? Screenshot { get; init; }
    public required string Suggestion { get; init; }
}

/// <summary>
/// An issue discovered during testing.
/// </summary>
public record DiscoveredIssue
{
    public required string Id { get; init; }
    public required IssueType Type { get; init; }
    public required IssueSeverity Severity { get; init; }
    public required string Description { get; init; }
    public string? ReproductionSteps { get; init; }
    public byte[]? Screenshot { get; init; }
    public string? StackTrace { get; init; }
    public required string SuggestedFix { get; init; }
}

/// <summary>
/// Type of issue discovered.
/// </summary>
public enum IssueType
{
    Bug,
    Crash,
    VisualGlitch,
    UX_Problem,
    Performance,
    Accessibility,
    Logic_Error,
    Missing_Feature,
    Broken_Flow
}

/// <summary>
/// Severity of an issue.
/// </summary>
public enum IssueSeverity
{
    Info,
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Actionable feedback item.
/// </summary>
public record ActionableFeedback
{
    public required string Id { get; init; }
    public required FeedbackPriority Priority { get; init; }
    public required string Problem { get; init; }
    public required string SuggestedAction { get; init; }
    public IReadOnlyList<string> AffectedFiles { get; init; } = Array.Empty<string>();
    public string? CodeSuggestion { get; init; }
}

/// <summary>
/// Priority of feedback.
/// </summary>
public enum FeedbackPriority
{
    Critical,   // Must fix before shipping
    High,       // Should fix before shipping
    Medium,     // Nice to fix
    Low         // Optional polish
}

/// <summary>
/// Evidence from testing.
/// </summary>
public record TestEvidence
{
    public required EvidenceType Type { get; init; }
    public required string Description { get; init; }
    public byte[]? ImageData { get; init; }
    public string? TextData { get; init; }
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Type of evidence.
/// </summary>
public enum EvidenceType
{
    Screenshot,
    Video,
    Log,
    Network,
    Console
}
