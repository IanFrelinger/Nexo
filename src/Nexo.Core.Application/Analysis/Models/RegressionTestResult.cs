namespace Nexo.Core.Application.Analysis.Models;

/// <summary>
/// Result of regression test run.
/// </summary>
public record RegressionTestResult
{
    public required bool AllPassed { get; init; }
    public int PassedCount { get; init; }
    public int FailedCount { get; init; }
    public string? Summary { get; init; }
}
