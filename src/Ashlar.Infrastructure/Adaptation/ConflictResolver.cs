using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Adaptation.Ports;

namespace Ashlar.Infrastructure.Adaptation;

/// <summary>
/// Resolves conflicts. Minimal: pick first candidate.
/// </summary>
public sealed class ConflictResolver : IConflictResolver
{
    /// <inheritdoc />
    public Task<BrickManifest?> ResolveAsync(IReadOnlyList<BrickManifest> candidates, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<BrickManifest?>(candidates.Count > 0 ? candidates[0] : null);
    }
}
