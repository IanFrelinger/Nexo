using Nexo.Core.Application.Certification.Models;

namespace Nexo.Core.Application.Certification.Ports;

/// <summary>
/// Composition-layer model seam — mirrors <see cref="Adaptation.Ports.IGeneratorModel"/> at the atom layer.
/// Receives <see cref="CompositionProposerInput"/> only (target signature + certified catalog); never witness cases.
/// </summary>
public interface ICompositionGeneratorModel
{
    /// <summary>
    /// Proposes a composition wiring using a language model or generator backend.
    /// </summary>
    /// <param name="input">Target I/O contract and admitted bricks (no witness cases).</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Proposed composition graph with provenance.</returns>
    Task<ProposedComposition> ProposeAsync(
        CompositionProposerInput input,
        CancellationToken cancellationToken = default);
}
