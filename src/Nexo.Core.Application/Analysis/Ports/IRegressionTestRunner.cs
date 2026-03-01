using Nexo.Core.Application.Analysis.Models;

namespace Nexo.Core.Application.Analysis.Ports;

/// <summary>
/// Runs regression tests to validate that adaptations don't break existing behavior.
/// </summary>
public interface IRegressionTestRunner
{
    /// <summary>
    /// Run tests and return whether all passed.
    /// </summary>
    /// <param name="projectOrSolutionPath">Path to .csproj or .sln.</param>
    /// <param name="filter">Optional test filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if all tests passed; false otherwise.</returns>
    Task<RegressionTestResult> RunAsync(
        string projectOrSolutionPath,
        string? filter = null,
        CancellationToken cancellationToken = default);
}
