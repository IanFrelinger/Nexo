using Nexo.Core.Application.Adaptation.Models;

namespace Nexo.Core.Application.Adaptation.Ports;

/// <summary>
/// Log of adaptation attempts. Records what was fixed, regression result, promoted or not.
/// </summary>
public interface IAdaptationLog
{
    /// <summary>Persists an adaptation attempt record.</summary>
    /// <param name="record">Adaptation outcome to log.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task LogAsync(AdaptationRecord record, CancellationToken cancellationToken = default);

    /// <summary>Queries adaptation records within an optional time range and brick filter.</summary>
    /// <param name="since">Earliest timestamp to include.</param>
    /// <param name="until">Latest timestamp to include.</param>
    /// <param name="brickId">Optional brick identifier filter.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<IReadOnlyList<AdaptationRecord>> QueryAsync(DateTimeOffset? since = null, DateTimeOffset? until = null, string? brickId = null, CancellationToken cancellationToken = default);
}
