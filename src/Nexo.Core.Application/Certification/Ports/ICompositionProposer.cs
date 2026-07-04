using Nexo.Core.Application.Certification.Models;

namespace Nexo.Core.Application.Certification.Ports;

/// <summary>
/// Untrusted composition proposer seam. Receives I/O signature + certified catalog only — never witness cases.
/// </summary>
public interface ICompositionProposer
{
    /// <summary>
    /// Proposes a composition wiring from the target signature and certified brick catalog.
    /// </summary>
    /// <param name="input">Target I/O contract and admitted bricks (no witness cases).</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Proposed composition graph with provenance.</returns>
    Task<ProposedComposition> ProposeAsync(
        CompositionProposerInput input,
        CancellationToken cancellationToken = default);
}
