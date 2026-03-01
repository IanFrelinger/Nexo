using Nexo.Core.Application.Adaptation.Models;

namespace Nexo.Core.Application.Adaptation.Ports;

/// <summary>
/// Aggregates results from parallel test instances.
/// </summary>
public interface IInstanceResultAggregator
{
    Task<AggregatedResult> AggregateAsync(IReadOnlyList<InstanceResult> results, CancellationToken cancellationToken = default);
}
