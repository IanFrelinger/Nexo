namespace Ashlar.Orchestration.Architect.Models;

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
