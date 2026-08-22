using Ashlar.Core.Application.Observation.Models;
using Ashlar.Core.Application.Observation.Ports;

namespace Ashlar.BackgroundAgents.Observation;

/// <summary>
/// No-op pattern store used when observation persistence cannot be initialized.
/// </summary>
public sealed class NoOpPatternStore : IPatternStore
{
    public Task AddAsync(ObservedPattern pattern, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<ObservedPattern>> QueryAsync(PatternStoreQueryParams query, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ObservedPattern>>([]);

    public Task PersistAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
