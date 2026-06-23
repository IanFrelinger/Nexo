using Nexo.Core.Application.Certification.Models;

namespace Nexo.Core.Application.Certification.Ports;

/// <summary>
/// Untrusted composition proposer seam. Receives I/O signature + certified catalog only — never witness cases.
/// </summary>
public interface ICompositionProposer
{
    Task<ProposedComposition> ProposeAsync(
        CompositionProposerInput input,
        CancellationToken cancellationToken = default);
}
