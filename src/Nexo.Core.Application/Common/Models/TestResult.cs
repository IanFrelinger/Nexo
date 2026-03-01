namespace Nexo.Core.Application.Common.Models;

/// <summary>
/// Unified result of a single test execution.
///
/// Contains:
/// - Test name and category
/// - Pass/fail status
/// - Optional message, error message, and stack trace
/// - Execution duration
/// - Optional metadata
///
/// Used by validation (TRX parsing), testing (TestExecutionResult), and test runners.
/// </summary>
public record TestResult
{
    public required string Name { get; init; }
    /// <summary>Alias for Name. Use Name for new code.</summary>
    public string TestName => Name;
    public required bool Passed { get; init; }
    public string? Message { get; init; }
    public string? Category { get; init; }
    public TimeSpan Duration { get; init; }
    public string? ErrorMessage { get; init; }
    public string? StackTrace { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}
