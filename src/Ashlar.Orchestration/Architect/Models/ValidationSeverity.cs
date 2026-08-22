namespace Ashlar.Orchestration.Architect.Models;

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
