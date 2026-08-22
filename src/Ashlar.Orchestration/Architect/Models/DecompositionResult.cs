namespace Ashlar.Orchestration.Architect.Models;

/// <summary>
/// Result of decomposing a request into agent specifications.
/// 
/// Contains:
/// - List of agent specifications to spawn
/// - Original request that was decomposed
/// - Reasoning/explanation for the decomposition
/// - Confidence score (0.0 to 1.0)
/// - Validation errors (if any)
/// 
/// Produced by ArchitectAgent during request decomposition.
/// Used by Orchestrator to spawn and execute agents.
/// </summary>
public sealed record DecompositionResult
{
    /// <summary>
    /// List of agent specifications that should be spawned to handle the request.
    /// </summary>
    public required IReadOnlyList<AgentSpawnSpec> Agents { get; init; }

    /// <summary>
    /// Original request that was decomposed.
    /// </summary>
    public required string OriginalRequest { get; init; }

    /// <summary>
    /// Reasoning or explanation for the decomposition.
    /// </summary>
    public string? Reasoning { get; init; }

    /// <summary>
    /// Confidence score (0.0 to 1.0) for the decomposition quality.
    /// </summary>
    public double Confidence { get; init; } = 0.0;

    /// <summary>
    /// Validation errors found during decomposition validation.
    /// </summary>
    public IReadOnlyList<ValidationError> ValidationErrors { get; init; } = Array.Empty<ValidationError>();

    /// <summary>
    /// Whether the decomposition passed all validations.
    /// </summary>
    public bool IsValid => ValidationErrors.Count == 0;
}
