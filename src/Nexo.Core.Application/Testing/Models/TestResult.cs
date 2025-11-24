namespace Nexo.Core.Application.Testing.Models;

/// <summary>
/// Result of a single test execution.
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

