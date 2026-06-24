using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;
using Nexo.Infrastructure.Certification.Composition;

namespace Nexo.Tests.Infrastructure.Certification.Doubles;

/// <summary>
/// <strong>TEST DOUBLE / REPLAY</strong> — hermetic stand-in for production model proposer.
/// Returns pre-recorded proposal(s) deterministically.
/// </summary>
internal sealed class RecordedCompositionGeneratorModel : ICompositionGeneratorModel
{
    private readonly IReadOnlyDictionary<string, RecordedCompositionProposal> _singleRecordings;
    private readonly RecordedCompositionProposalBatch? _batch;

    public RecordedCompositionGeneratorModel(
        IEnumerable<RecordedCompositionProposal> singleRecordings,
        RecordedCompositionProposalBatch? batch = null)
    {
        _singleRecordings = singleRecordings.ToDictionary(r => r.CompositionId, StringComparer.Ordinal);
        _batch = batch;
    }

    public RecordedCompositionProposalBatch? Batch => _batch;

    public static RecordedCompositionGeneratorModel FromJsonFiles(params string[] paths)
    {
        var recordings = paths.Select(path =>
        {
            var json = File.ReadAllText(path);
            return RecordedCompositionProposal.FromJson(json);
        });
        return new RecordedCompositionGeneratorModel(recordings);
    }

    public static RecordedCompositionGeneratorModel FromBatchFile(string path)
    {
        var batch = RecordedCompositionProposalBatch.FromJson(File.ReadAllText(path));
        return new RecordedCompositionGeneratorModel([], batch);
    }

    public Task<ProposedComposition> ProposeAsync(
        CompositionProposerInput input,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_singleRecordings.TryGetValue(input.Target.CompositionId, out var recording))
        {
            throw new NotSupportedException(
                $"Recorded composition model has no recording for '{input.Target.CompositionId}'.");
        }

        return Task.FromResult(new ProposedComposition(recording.Spec, recording.Provenance));
    }

    public RecordedCompositionProposal? GetRecording(string compositionId) =>
        _singleRecordings.TryGetValue(compositionId, out var recording) ? recording : null;

    public IReadOnlyList<RecordedCompositionBatchEntry> GetBatchEntries() =>
        _batch?.Entries ?? [];
}
