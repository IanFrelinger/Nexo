using Nexo.Core.Application.Certification.Models;

namespace Nexo.Core.Application.Certification.Ports;

/// <summary>
/// Composition-layer model seam — mirrors <see cref="Adaptation.Ports.IGeneratorModel"/> at the atom layer.
/// Receives <see cref="CompositionProposerInput"/> only (target signature + certified catalog); never witness cases.
/// </summary>
public interface ICompositionGeneratorModel
{
    Task<ProposedComposition> ProposeAsync(
        CompositionProposerInput input,
        CancellationToken cancellationToken = default);
}
