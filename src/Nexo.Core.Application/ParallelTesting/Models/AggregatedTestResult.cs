namespace Nexo.Core.Application.ParallelTesting.Models;

/// <summary>
/// Aggregated result from parallel test runs.
/// </summary>
public record AggregatedTestResult
{
    public required IReadOnlyList<TestInstance> Results { get; init; }
    public TestInstance? BestCandidate { get; init; }
    public bool AllPassed { get; init; }
}
