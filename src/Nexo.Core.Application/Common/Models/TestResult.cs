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
    /// <summary>Display name of the test.</summary>
    public required string Name { get; init; }

    /// <summary>Alias for <see cref="Name"/>. Prefer <see cref="Name"/> in new code.</summary>
    public string TestName => Name;

    /// <summary>Whether the test passed.</summary>
    public required bool Passed { get; init; }

    /// <summary>Optional informational message from the test run.</summary>
    public string? Message { get; init; }

    /// <summary>Test category or trait label, when available.</summary>
    public string? Category { get; init; }

    /// <summary>Wall-clock duration of the test execution.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Failure message when <see cref="Passed"/> is false.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Stack trace captured on failure, when available.</summary>
    public string? StackTrace { get; init; }

    /// <summary>Additional key-value metadata from the test runner.</summary>
    public Dictionary<string, object>? Metadata { get; init; }
}
