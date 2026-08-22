using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Domain.Bricks;

namespace Ashlar.Core.Application.Adaptation.Ports;

/// <summary>
/// Recompiles an edited BrickManifest into a deployable brick.
/// </summary>
public interface IBrickRecompiler
{
    /// <summary>
    /// Recompile a manifest into a runnable brick.
    /// </summary>
    /// <param name="manifest">The edited manifest.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The compiled brick, or null if recompilation failed.</returns>
    Task<DomainBrick?> RecompileAsync(BrickManifest manifest, CancellationToken cancellationToken = default);
}
