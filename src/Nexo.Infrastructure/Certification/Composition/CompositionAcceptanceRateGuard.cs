namespace Nexo.Infrastructure.Certification.Composition;

/// <summary>
/// Zero/short-batch guard for acceptance-rate measurement (analog of zero-mutant guard).
/// </summary>
public static class CompositionAcceptanceRateGuard
{
    public static void EnsureBatchComplete(RecordedCompositionProposalBatch batch)
    {
        if (batch.DeclaredSampleCount <= 0)
            throw new InvalidOperationException("Declared sample count must be positive.");

        if (batch.Entries.Count == 0)
            throw new InvalidOperationException("Empty batch — acceptance rate is vacuous.");

        if (batch.Entries.Count < batch.DeclaredSampleCount)
        {
            throw new InvalidOperationException(
                $"Batch truncated: {batch.Entries.Count} entries < declared N={batch.DeclaredSampleCount}.");
        }

        if (batch.Entries.Count > batch.DeclaredSampleCount)
        {
            throw new InvalidOperationException(
                $"Batch oversized: {batch.Entries.Count} entries > declared N={batch.DeclaredSampleCount}.");
        }

        var indices = batch.Entries.Select(e => e.SequenceIndex).OrderBy(i => i).ToList();
        for (var i = 0; i < batch.DeclaredSampleCount; i++)
        {
            if (indices[i] != i)
            {
                throw new InvalidOperationException(
                    $"Batch sequence gap: expected index {i}, found {indices[i]}.");
            }
        }
    }
}
