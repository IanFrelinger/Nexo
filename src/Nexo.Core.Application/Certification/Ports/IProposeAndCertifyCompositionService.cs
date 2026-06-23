using Nexo.Core.Application.Certification.Models;

namespace Nexo.Core.Application.Certification.Ports;

public sealed record ProposeCertifyCompositionResult(
    ProposedComposition Proposal,
    CompositionCertificationDecision Decision);

/// <summary>
/// Propose→certify loop: untrusted proposer output traverses the existing composition gate unchanged.
/// </summary>
public interface IProposeAndCertifyCompositionService
{
    Task<ProposeCertifyCompositionResult> ProposeCertifyAndAdmitAsync(
        CompositionProposerInput proposerInput,
        CompositionWitnessSpec witness,
        string? wiringMetadata = null,
        CancellationToken cancellationToken = default);
}
