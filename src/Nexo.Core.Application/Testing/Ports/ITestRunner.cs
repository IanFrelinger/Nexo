using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Application.Common.Models;

namespace Nexo.Core.Application.Testing.Ports;

/// <summary>
/// Port for running tests.
/// 
/// Defines the contract for test execution:
/// - Run all tests or tests matching a filter
/// - Report progress through IProgress&lt;ProgressReport&gt;
/// - Returns TestExecutionResult with aggregated results
/// 
/// Implementations (TestRunnerAdapter) provide test discovery and execution.
/// Used by CLI commands to run tests.
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

