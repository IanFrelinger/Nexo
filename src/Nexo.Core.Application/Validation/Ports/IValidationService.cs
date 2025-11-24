using Nexo.Core.Application.Validation.Models;
using Nexo.Core.Application.Common.Models;

namespace Nexo.Core.Application.Validation.Ports;

/// <summary>
/// Port for running architecture tests and contract checks.
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Runs validation tests with an optional filter.
    /// </summary>
    /// <param name="filter">Optional test filter.</param>
    /// <param name="progress">Optional progress reporter for streaming updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ValidationResult> ValidateAsync(
        string? filter,
        IProgress<ProgressReport>? progress = null,
        CancellationToken cancellationToken = default);
}

