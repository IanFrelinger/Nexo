using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Application.Certification.Ports;

namespace Ashlar.Infrastructure.Certification.Composition;

/// <summary>
/// Propose→certify loop. Proposals traverse the existing composition admission gate unchanged.
/// </summary>
public sealed class ProposeAndCertifyCompositionService : IProposeAndCertifyCompositionService
{
    private readonly ICompositionProposer _proposer;
    private readonly ICertifiedCompositionAdmission _admission;

    /// <summary>Initializes a new propose and certify composition service.</summary>
    public ProposeAndCertifyCompositionService(
        ICompositionProposer proposer,
        ICertifiedCompositionAdmission admission)
    {
        _proposer = proposer ?? throw new ArgumentNullException(nameof(proposer));
        _admission = admission ?? throw new ArgumentNullException(nameof(admission));
    }

    /// <summary>Propose certify and admit asynchronously.</summary>
    public async Task<ProposeCertifyCompositionResult> ProposeCertifyAndAdmitAsync(
        CompositionProposerInput proposerInput,
        CompositionWitnessSpec witness,
        string? wiringMetadata = null,
        CancellationToken cancellationToken = default)
    {
        var proposal = await _proposer.ProposeAsync(proposerInput, cancellationToken).ConfigureAwait(false);

        var request = new CompositionCertificationRequest
        {
            Spec = proposal.Spec with { CompositionId = witness.CompositionId },
            Witness = witness,
            WiringMetadata = wiringMetadata
        };

        var decision = await _admission.CertifyAndAdmitAsync(request, cancellationToken).ConfigureAwait(false);
        return new ProposeCertifyCompositionResult(proposal, decision);
    }
}
