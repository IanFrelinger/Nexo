using Ashlar.Core.Application.ParallelTesting.Models;
using Ashlar.Core.Application.ParallelTesting.Ports;

namespace Ashlar.Infrastructure.ParallelTesting;

/// <summary>
/// Collects and aggregates test instance results.
/// </summary>
public sealed class ResultCollector : IResultCollector
{
    /// <inheritdoc />
    public Task<AggregatedTestResult> CollectAsync(IReadOnlyList<TestInstance> instances, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Empty matrix used to vacuous-pass: Enumerable.All is true on zero items.
        var allPassed = instances.Count > 0 && instances.All(i => i.Passed);
        var best = instances.FirstOrDefault(i => i.Passed) ?? instances.FirstOrDefault();
        return Task.FromResult(new AggregatedTestResult
        {
            Results = instances,
            BestCandidate = best,
            AllPassed = allPassed,
        });
    }
}
