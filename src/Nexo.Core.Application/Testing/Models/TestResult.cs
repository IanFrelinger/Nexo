using Nexo.Core.Application.Common.Models;

namespace Nexo.Core.Application.Testing.Models;

/// <summary>
/// Aggregated test execution results.
/// 
/// Contains:
/// - Total test counts (total, passed, failed)
/// - Total execution duration
/// - List of individual test results
/// - Optional list of test categories
/// 
/// Produced by ITestRunner after executing tests.
/// Used by CLI commands to display test execution results.
/// </summary>
public record TestExecutionResult
{
    /// <summary>Total number of tests executed.</summary>
    public required int TotalTests { get; init; }

    /// <summary>Number of tests that passed.</summary>
    public required int PassedTests { get; init; }

    /// <summary>Number of tests that failed.</summary>
    public required int FailedTests { get; init; }

    /// <summary>Combined wall-clock duration of the test run.</summary>
    public required TimeSpan TotalDuration { get; init; }

    /// <summary>Individual test results.</summary>
    public required IReadOnlyList<TestResult> Results { get; init; }

    /// <summary>Distinct test categories present in the run, when available.</summary>
    public IReadOnlyList<string>? Categories { get; init; }
}
