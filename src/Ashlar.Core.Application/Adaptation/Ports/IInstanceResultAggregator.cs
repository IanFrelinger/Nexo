using Ashlar.Core.Application.Adaptation.Models;

namespace Ashlar.Core.Application.Adaptation.Ports;

/// <summary>
/// Aggregates results from parallel test instances.
/// </summary>
public interface IInstanceResultAggregator
{
    /// <summary>
    /// Aggregates parallel instance results into a single outcome with best candidate selection.
    /// </summary>
    /// <param name="results">Individual instance results to aggregate.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Aggregated result with pass/fail summary.</returns>
    Task<AggregatedResult> AggregateAsync(IReadOnlyList<InstanceResult> results, CancellationToken cancellationToken = default);
}
