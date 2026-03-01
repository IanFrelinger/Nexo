using Nexo.Core.Application.Adaptation.Models;

namespace Nexo.Core.Application.Adaptation.Ports;

/// <summary>
/// Resolves conflicts when multiple instances generated different fixes for the same gap.
/// </summary>
public interface IConflictResolver
{
    Task<BrickManifest?> ResolveAsync(IReadOnlyList<BrickManifest> candidates, CancellationToken cancellationToken = default);
}
