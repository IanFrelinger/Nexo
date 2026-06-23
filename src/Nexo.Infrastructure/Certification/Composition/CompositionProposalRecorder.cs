using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;

namespace Nexo.Infrastructure.Certification.Composition;

/// <summary>
/// Persists a live model proposal and gate verdict for offline replay.
/// Run locally once — never invoked from blocking cert-gate.
/// </summary>
public static class CompositionProposalRecorder
{
    public static async Task SaveAsync(
        string outputPath,
        ProposedComposition proposal,
        CompositionCertificationDecision gateDecision,
        string provider,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var recording = new RecordedCompositionProposal(
            proposal.Provenance,
            provider,
            DateTimeOffset.UtcNow,
            proposal.Spec.CompositionId,
            gateDecision.Admitted,
            gateDecision.Admitted ? "ADMIT" : "REJECT",
            gateDecision.Admitted ? null : gateDecision.FailureCheck,
            proposal.Spec);

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(outputPath, recording.ToJson(), cancellationToken).ConfigureAwait(false);
    }
}
