using Nexo.Core.Application.Validation.Models;

namespace Nexo.Core.Application.Validation.Ports;

/// <summary>
/// Port for running architecture tests and contract checks.
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Runs validation tests with an optional filter.
    /// </summary>
    Task<ValidationResult> ValidateAsync(
        string? filter,
        CancellationToken cancellationToken = default);
}

