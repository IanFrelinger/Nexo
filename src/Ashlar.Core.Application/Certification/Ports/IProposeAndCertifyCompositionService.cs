using Ashlar.Core.Application.Certification.Models;

namespace Ashlar.Core.Application.Certification.Ports;

/// <summary>
/// Propose→certify loop: untrusted proposer output traverses the existing composition gate unchanged.
/// </summary>
public interface IProposeAndCertifyCompositionService
{
    /// <summary>
    /// Proposes a composition from certified bricks, certifies it, and admits on success.
    /// </summary>
    /// <param name="proposerInput">Target signature and certified brick catalog (no witness cases).</param>
    /// <param name="witness">Independent end-to-end witness for certification.</param>
    /// <param name="wiringMetadata">Optional provenance metadata from the proposer.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Proposal and certification decision.</returns>
    Task<ProposeCertifyCompositionResult> ProposeCertifyAndAdmitAsync(
        CompositionProposerInput proposerInput,
        CompositionWitnessSpec witness,
        string? wiringMetadata = null,
        CancellationToken cancellationToken = default);
}
