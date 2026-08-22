using Ashlar.Core.Application.Adaptation.Models;

namespace Ashlar.Core.Application.Adaptation.Ports;

/// <summary>
/// Generates candidate fixes from failure context.
/// </summary>
public interface IFixGenerator
{
    /// <summary>
    /// Generate candidate brick manifests (fixes) for the given failure context.
    /// </summary>
    /// <param name="context">The failure context.</param>
    /// <param name="baseManifest">Optional base manifest to modify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<BrickManifest>> GenerateFixesAsync(
        FailureContext context,
        BrickManifest? baseManifest = null,
        CancellationToken cancellationToken = default);
}
