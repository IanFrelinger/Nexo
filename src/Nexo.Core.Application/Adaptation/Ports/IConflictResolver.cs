using Nexo.Core.Application.Adaptation.Models;

namespace Nexo.Core.Application.Adaptation.Ports;

/// <summary>
/// Resolves conflicts when multiple instances generated different fixes for the same gap.
/// </summary>
public interface IConflictResolver
{
    /// <summary>
    /// Selects the best candidate manifest from competing fixes.
    /// </summary>
    /// <param name="candidates">Competing brick manifest fixes.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Resolved manifest, or null when no candidate is acceptable.</returns>
    Task<BrickManifest?> ResolveAsync(IReadOnlyList<BrickManifest> candidates, CancellationToken cancellationToken = default);
}
