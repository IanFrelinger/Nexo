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
    public required int TotalTests { get; init; }
    public required int PassedTests { get; init; }
    public required int FailedTests { get; init; }
    public required TimeSpan TotalDuration { get; init; }
    public required IReadOnlyList<TestResult> Results { get; init; }
    public IReadOnlyList<string>? Categories { get; init; }
}

