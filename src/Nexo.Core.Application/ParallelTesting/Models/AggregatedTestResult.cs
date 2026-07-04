namespace Nexo.Core.Application.ParallelTesting.Models;

/// <summary>Aggregated result from parallel test runs.</summary>
public record AggregatedTestResult
{
    /// <summary>Individual test instance results from the parallel run.</summary>
    public required IReadOnlyList<TestInstance> Results { get; init; }

    /// <summary>Best-performing instance when ranking is applied.</summary>
    public TestInstance? BestCandidate { get; init; }

    /// <summary>Whether every parallel instance passed.</summary>
    public bool AllPassed { get; init; }
}
