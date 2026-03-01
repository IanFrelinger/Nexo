using Nexo.Core.Application.Common.Models;

namespace Nexo.Core.Application.Validation.Models;

/// <summary>
/// Result of a validation operation.
/// 
/// Contains:
/// - Whether validation passed
/// - Summary message
/// - Test execution statistics (run, passed, failed)
/// - Optional list of individual test results
/// 
/// Produced by IValidationService after running validation tests.
/// Used by CLI commands to display validation results.
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

