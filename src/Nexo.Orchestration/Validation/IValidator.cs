namespace Nexo.Orchestration.Validation;

using Architect.Models;

/// <summary>
/// Interface for validators that check decomposition results for correctness.
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

