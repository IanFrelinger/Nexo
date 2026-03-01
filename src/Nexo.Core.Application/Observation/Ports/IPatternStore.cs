using Nexo.Core.Application.Observation.Models;

namespace Nexo.Core.Application.Observation.Ports;

/// <summary>
/// Persistent, queryable store for observed patterns. Survives restarts.
/// </summary>
public interface IPatternStore
{
    /// <summary>Add a pattern to the store.</summary>
    Task AddAsync(ObservedPattern pattern, CancellationToken cancellationToken = default);

    /// <summary>Query patterns matching the given parameters.</summary>
    Task<IReadOnlyList<ObservedPattern>> QueryAsync(PatternStoreQueryParams query, CancellationToken cancellationToken = default);

    /// <summary>Persist any buffered data (no-op for immediate persistence).</summary>
    Task PersistAsync(CancellationToken cancellationToken = default);
}
