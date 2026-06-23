using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;

namespace Nexo.Infrastructure.Certification.Composition;

/// <summary>
/// <strong>TEST DOUBLE / REPLAY</strong> — hermetic stand-in for <see cref="ProviderCompositionGeneratorModel"/>.
/// Returns a pre-recorded proposal deterministically; mirrors <see cref="Adaptation.Generation.FixtureGeneratorModel"/>.
/// </summary>
public sealed class RecordedCompositionGeneratorModel : ICompositionGeneratorModel
{
    private readonly IReadOnlyDictionary<string, RecordedCompositionProposal> _recordings;

    public RecordedCompositionGeneratorModel(IEnumerable<RecordedCompositionProposal> recordings)
    {
        _recordings = recordings.ToDictionary(r => r.CompositionId, StringComparer.Ordinal);
    }

    public static RecordedCompositionGeneratorModel FromJsonFiles(params string[] paths)
    {
        var recordings = paths.Select(path =>
        {
            var json = File.ReadAllText(path);
            return RecordedCompositionProposal.FromJson(json);
        });
        return new RecordedCompositionGeneratorModel(recordings);
    }

    public Task<ProposedComposition> ProposeAsync(
        CompositionProposerInput input,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_recordings.TryGetValue(input.Target.CompositionId, out var recording))
        {
            throw new NotSupportedException(
                $"Recorded composition model has no recording for '{input.Target.CompositionId}'.");
        }

        return Task.FromResult(new ProposedComposition(recording.Spec, recording.Provenance));
    }

    public RecordedCompositionProposal? GetRecording(string compositionId) =>
        _recordings.TryGetValue(compositionId, out var recording) ? recording : null;
}
