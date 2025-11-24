namespace Nexo.Core.Application.Validation.Models;

/// <summary>
/// Result of a validation operation.
/// </summary>
public record ValidationResult
{
    public required bool Passed { get; init; }
    public required string Message { get; init; }
    public required int TestsRun { get; init; }
    public required int TestsPassed { get; init; }
    public required int TestsFailed { get; init; }
    public IReadOnlyList<TestResult>? TestResults { get; init; }
}

/// <summary>
/// Represents a single test result.
/// </summary>
public record TestResult
{
    public required string Name { get; init; }
    public required bool Passed { get; init; }
    public string? Message { get; init; }
    public string? Category { get; init; }
}

