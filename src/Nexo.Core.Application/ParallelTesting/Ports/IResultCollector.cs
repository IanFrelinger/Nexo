using Nexo.Core.Application.ParallelTesting.Models;

namespace Nexo.Core.Application.ParallelTesting.Ports;

/// <summary>
/// Collects and aggregates results from test instances.
/// </summary>
public interface IResultCollector
{
    Task<AggregatedTestResult> CollectAsync(IReadOnlyList<TestInstance> instances, CancellationToken cancellationToken = default);
}
