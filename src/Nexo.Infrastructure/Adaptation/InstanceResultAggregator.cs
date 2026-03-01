using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;

namespace Nexo.Infrastructure.Adaptation;

/// <summary>
/// Aggregates instance results. Stub: single-instance pass-through.
/// </summary>
public sealed class InstanceResultAggregator : IInstanceResultAggregator
{
    /// <inheritdoc />
    public Task<AggregatedResult> AggregateAsync(IReadOnlyList<InstanceResult> results, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var allPassed = results.All(r => r.Passed);
        var best = results.FirstOrDefault(r => r.Passed) ?? results.FirstOrDefault();
        return Task.FromResult(new AggregatedResult
        {
            Results = results,
            BestCandidate = best,
            AllPassed = allPassed,
        });
    }
}
