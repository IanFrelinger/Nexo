using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;

namespace Nexo.Infrastructure.Certification.Composition;

/// <summary>
/// Persists live model proposals and gate verdicts for offline replay.
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

        var capturedAt = DateTimeOffset.UtcNow;
        var recording = new RecordedCompositionProposal(
            proposal.Provenance,
            provider,
            capturedAt,
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

    public static async Task SaveBatchAsync(
        string outputPath,
        RecordedCompositionProposalBatch batch,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(outputPath, batch.ToJson(), cancellationToken).ConfigureAwait(false);
    }
}
