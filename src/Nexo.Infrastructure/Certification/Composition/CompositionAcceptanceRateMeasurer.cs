using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;

namespace Nexo.Infrastructure.Certification.Composition;

/// <summary>
/// Measures raw proposer acceptance rate over a recorded batch via the unchanged propose→certify loop.
/// Reports admits/total; never asserts a target rate.
/// </summary>
public static class CompositionAcceptanceRateMeasurer
{
    public static async Task<CompositionAcceptanceRateResult> MeasureRecordedBatchAsync(
        RecordedCompositionProposalBatch batch,
        CompositionProposerInput proposerInput,
        CompositionWitnessSpec witness,
        ICertifiedCompositionAdmission admission,
        string? wiringMetadata = null,
        CancellationToken cancellationToken = default)
    {
        CompositionAcceptanceRateGuard.EnsureBatchComplete(batch);

        var entryResults = new List<CompositionAcceptanceRateEntryResult>();
        var admits = 0;

        foreach (var entry in batch.Entries.OrderBy(e => e.SequenceIndex))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var service = new ProposeAndCertifyCompositionService(
                new RecordedEntryReplayProposer(entry),
                admission);

            var result = await service.ProposeCertifyAndAdmitAsync(
                proposerInput,
                witness,
                wiringMetadata,
                cancellationToken).ConfigureAwait(false);

            if (result.Decision.Admitted != entry.GateAdmittedAtRecording)
            {
                throw new InvalidOperationException(
                    $"Sequence {entry.SequenceIndex}: replay verdict {FormatVerdict(result.Decision)} " +
                    $"!= recorded {entry.GateVerdictAtRecording}.");
            }

            if (!result.Decision.Admitted
                && entry.GateFailureCheckAtRecording is not null
                && !string.Equals(result.Decision.FailureCheck, entry.GateFailureCheckAtRecording, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Sequence {entry.SequenceIndex}: replay failure check '{result.Decision.FailureCheck}' " +
                    $"!= recorded '{entry.GateFailureCheckAtRecording}'.");
            }

            if (result.Decision.Admitted)
                admits++;

            entryResults.Add(new CompositionAcceptanceRateEntryResult(
                entry.SequenceIndex,
                entry.GateAdmittedAtRecording,
                result.Decision.Admitted,
                entry.GateFailureCheckAtRecording,
                result.Decision.Admitted ? null : result.Decision.FailureCheck));
        }

        var total = batch.Entries.Count;
        var rate = total == 0 ? 0d : (double)admits / total;
        return new CompositionAcceptanceRateResult(rate, admits, total, entryResults);
    }

    public static string FormatRate(double acceptanceRate) => acceptanceRate.ToString("F2");

    private static string FormatVerdict(CompositionCertificationDecision decision) =>
        decision.Admitted ? "ADMIT" : $"REJECT({decision.FailureCheck})";

    private sealed class RecordedEntryReplayProposer(RecordedCompositionBatchEntry entry) : ICompositionProposer
    {
        public Task<ProposedComposition> ProposeAsync(
            CompositionProposerInput input,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new ProposedComposition(entry.Spec, entry.Provenance));
        }
    }
}
