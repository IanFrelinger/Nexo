using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;

namespace Nexo.Infrastructure.Adaptation;

/// <summary>
/// Aggregates instance results. For multi-instance runs: prefers passed results with shortest duration;
/// among failures, prefers those with fewer adaptations (closer to fix).
/// </summary>
public sealed class InstanceResultAggregator : IInstanceResultAggregator
{
    /// <inheritdoc />
    public Task<AggregatedResult> AggregateAsync(IReadOnlyList<InstanceResult> results, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var allPassed = results.All(r => r.Passed);
        var best = SelectBestCandidate(results);
        return Task.FromResult(new AggregatedResult
        {
            Results = results,
            BestCandidate = best,
            AllPassed = allPassed,
        });
    }

    private static InstanceResult? SelectBestCandidate(IReadOnlyList<InstanceResult> results)
    {
        if (results.Count == 0) return null;
        var passed = results.Where(r => r.Passed).ToList();
        if (passed.Count > 0)
            return passed.MinBy(r => r.Duration) ?? passed[0];
        var failed = results.ToList();
        return failed.MinBy(r => r.Adaptations?.Count ?? int.MaxValue) ?? failed[0];
    }
}
