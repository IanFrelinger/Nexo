namespace Nexo.Core.Application.Testing.Models;

/// <summary>
/// Result of a single test execution.
/// 
/// Contains:
/// - Test name and category
/// - Pass/fail status
/// - Optional message, error message, and stack trace
/// - Execution duration
/// - Optional metadata
/// 
/// Produced by TestBase.ExecuteAsync.
/// Used by ITestRunner to aggregate test results.
/// </summary>
public record TestResult
{
    public required string TestName { get; init; }
    public required string Category { get; init; }
    public required bool Passed { get; init; }
    public string? Message { get; init; }
    public TimeSpan Duration { get; init; }
    public string? ErrorMessage { get; init; }
    public string? StackTrace { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}

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

