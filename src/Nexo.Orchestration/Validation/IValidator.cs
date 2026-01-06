namespace Nexo.Orchestration.Validation;

using Architect.Models;

/// <summary>
/// Interface for validators that check decomposition results for correctness.
/// 
/// Validators are used by ArchitectAgent to validate decomposition results:
/// - SchemaValidator: Validates JSON schema conformance
/// - DependencyAnalyzer: Detects cycles and missing dependencies
/// - CoverageChecker: Ensures all requirements are covered
/// - ConstraintSolver: Detects contradictory constraints
/// 
/// All validators run in parallel during decomposition validation.
/// </summary>
public interface IValidator
{
    /// <summary>
    /// Validates a decomposition result.
    /// </summary>
    /// <param name="result">The decomposition result to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of validation errors found (empty if valid).</returns>
    Task<IReadOnlyList<ValidationError>> ValidateAsync(
        DecompositionResult result,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Name of this validator (for logging and debugging).
    /// </summary>
    string Name { get; }
}

