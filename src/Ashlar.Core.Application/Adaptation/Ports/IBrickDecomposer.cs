using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Domain.Bricks;

namespace Ashlar.Core.Application.Adaptation.Ports;

/// <summary>
/// Decomposes an existing brick into an editable representation.
/// </summary>
public interface IBrickDecomposer
{
    /// <summary>
    /// Decompose a brick into a BrickManifest that can be edited and recompiled.
    /// </summary>
    /// <param name="brick">The brick to decompose.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<BrickManifest> DecomposeAsync(DomainBrick brick, CancellationToken cancellationToken = default);
}
