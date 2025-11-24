using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Application.Common.Models;

namespace Nexo.Core.Application.Testing.Ports;

/// <summary>
/// Port for running tests.
/// </summary>
public interface ITestRunner
{
    /// <summary>
    /// Runs all tests or tests matching the specified filter.
    /// </summary>
    Task<TestExecutionResult> RunTestsAsync(
        string? filter = null,
        IProgress<ProgressReport>? progress = null,
        CancellationToken cancellationToken = default);
}

