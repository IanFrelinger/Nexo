namespace Nexo.Agents.AutonomousDev.Models;

/// <summary>
/// AI's interpretation of what needs to be built.
/// </summary>
public record Specification
{
    /// <summary>
    /// Clear, actionable summary of the task.
    /// </summary>
    public required string Summary { get; init; }
    
    /// <summary>
    /// What type of change is this?
    /// </summary>
    public required ChangeType ChangeType { get; init; }
    
    /// <summary>
    /// Functional requirements - what it must do.
    /// </summary>
    public IReadOnlyList<Requirement> FunctionalRequirements { get; init; } = Array.Empty<Requirement>();
    
    /// <summary>
    /// Non-functional requirements - how it must perform.
    /// </summary>
    public IReadOnlyList<Requirement> NonFunctionalRequirements { get; init; } = Array.Empty<Requirement>();
    
    /// <summary>
    /// Acceptance criteria - how we know it's done.
    /// </summary>
    public IReadOnlyList<AcceptanceCriterion> AcceptanceCriteria { get; init; } = Array.Empty<AcceptanceCriterion>();
    
    /// <summary>
    /// What parts of the codebase will likely need changes?
    /// </summary>
    public IReadOnlyList<string> AffectedAreas { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// Potential risks or complications.
    /// </summary>
    public IReadOnlyList<string> Risks { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// Estimated complexity (1-10).
    /// </summary>
    public int EstimatedComplexity { get; init; }
    
    /// <summary>
    /// Confidence in this interpretation (0-1).
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// Questions or ambiguities that need clarification.
    /// </summary>
    public IReadOnlyList<string> OpenQuestions { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Type of change being made.
/// </summary>
public enum ChangeType
{
    NewFeature,
    BugFix,
    Refactor,
    Performance,
    UI_UX,
    Security,
    Configuration,
    Documentation,
    Test
}

/// <summary>
/// A requirement for the feature.
/// </summary>
public record Requirement
{
    public required string Id { get; init; }
    public required string Description { get; init; }
    public RequirementPriority Priority { get; init; }
    public bool IsMandatory { get; init; }
}

/// <summary>
/// Priority of a requirement.
/// </summary>
public enum RequirementPriority
{
    Must,
    Should,
    Could,
    Wont
}

/// <summary>
/// Acceptance criterion for completion.
/// </summary>
public record AcceptanceCriterion
{
    public required string Id { get; init; }
    public required string Description { get; init; }
    public required string TestDescription { get; init; }
    public bool IsAutomatable { get; init; }
}
