using Nexo.Core.Application.Adaptation.Models;

namespace Nexo.Core.Application.Adaptation.Ports;

/// <summary>
/// Pulls shared adaptations from peers and adopts them after validation.
/// P2.3: nexo mesh sync uses this to pull and validate before adoption.
/// </summary>
public interface ISharedAdaptationSync
{
    /// <summary>
    /// Pulls available shared adaptations from trusted peers.
    /// </summary>
    Task<IReadOnlyList<SharedAdaptationEntry>> PullAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the adaptation (e.g. runs regression tests) and adopts it if valid.
    /// Writes files and promotes the record.
    /// </summary>
    /// <returns>True if adopted; false if validation failed.</returns>
    Task<bool> ValidateAndAdoptAsync(SharedAdaptationEntry entry, CancellationToken cancellationToken = default);
}
