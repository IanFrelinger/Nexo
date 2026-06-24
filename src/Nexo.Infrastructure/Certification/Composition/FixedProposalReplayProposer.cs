using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;

namespace Nexo.Infrastructure.Certification.Composition;

/// <summary>
/// Replays one recorded batch entry through <see cref="ICompositionProposer"/>.
/// </summary>
internal sealed class FixedProposalReplayProposer : ICompositionProposer
{
    private readonly RecordedCompositionBatchEntry _entry;

    public FixedProposalReplayProposer(RecordedCompositionBatchEntry entry) =>
        _entry = entry ?? throw new ArgumentNullException(nameof(entry));

    public Task<ProposedComposition> ProposeAsync(
        CompositionProposerInput input,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new ProposedComposition(_entry.Spec, _entry.Provenance));
    }
}
