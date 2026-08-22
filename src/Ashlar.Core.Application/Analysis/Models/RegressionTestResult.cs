namespace Ashlar.Core.Application.Analysis.Models;

/// <summary>Result of regression test run.</summary>
public record RegressionTestResult
{
    /// <summary>Whether all regression tests passed.</summary>
    public required bool AllPassed { get; init; }

    /// <summary>Number of tests that passed.</summary>
    public int PassedCount { get; init; }

    /// <summary>Number of tests that failed.</summary>
    public int FailedCount { get; init; }

    /// <summary>Optional human-readable summary of the test run.</summary>
    public string? Summary { get; init; }
}
