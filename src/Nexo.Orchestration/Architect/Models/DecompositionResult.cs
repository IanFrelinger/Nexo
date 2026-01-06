namespace Nexo.Orchestration.Architect.Models;

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

/// <summary>
/// Represents a validation error found during decomposition validation.
/// 
/// Contains:
/// - Error type (Schema, Dependency, Coverage, Constraint)
/// - Human-readable error message
/// - Agent ID where error occurred (if applicable)
/// - Severity level (Info, Warning, Error)
/// 
/// Used by validators to report issues with decomposition results.
/// </summary>
public sealed record ValidationError
{
    /// <summary>
    /// Type of validation error (e.g., "Schema", "Dependency", "Coverage", "Constraint").
    /// </summary>
    public required string ErrorType { get; init; }

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Agent ID or path where the error occurred (if applicable).
    /// </summary>
    public string? AgentId { get; init; }

    /// <summary>
    /// Severity of the error.
    /// </summary>
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;
}

/// <summary>
/// Severity level of a validation error.
/// 
/// Defines the severity of validation issues:
/// - Info: Informational message (not an error)
/// - Warning: Should be addressed but doesn't block execution
/// - Error: Blocks execution
/// 
/// Used by ValidationError to indicate issue severity.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Informational message (not an error).
    /// </summary>
    Info,

    /// <summary>
    /// Warning that should be addressed but doesn't block execution.
    /// </summary>
    Warning,

    /// <summary>
    /// Error that blocks execution.
    /// </summary>
    Error
}

