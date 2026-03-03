using Nexo.Core.Application.Adaptation.Models;

namespace Nexo.Core.Application.Adaptation.Ports;

/// <summary>
/// Broadcasts promoted adaptations to trusted peers for the shared adaptation cache.
/// P2.3: When an adaptation is promoted, it is broadcast so peers can pull and adopt.
/// </summary>
public interface ISharedAdaptationBroadcaster
{
    /// <summary>
    /// Broadcasts a promoted adaptation to the shared store for peers to pull.
    /// </summary>
    Task BroadcastAsync(SharedAdaptationEntry entry, CancellationToken cancellationToken = default);
}
