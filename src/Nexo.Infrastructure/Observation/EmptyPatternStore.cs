using Nexo.Core.Application.Observation.Models;
using Nexo.Core.Application.Observation.Ports;

namespace Nexo.Infrastructure.Observation;

/// <summary>
/// No-op pattern store. Returns empty results.
/// </summary>
public sealed class EmptyPatternStore : IPatternStore
{
    public Task AddAsync(ObservedPattern pattern, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<IReadOnlyList<ObservedPattern>> QueryAsync(PatternStoreQueryParams query, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ObservedPattern>>(Array.Empty<ObservedPattern>());
    public Task PersistAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
