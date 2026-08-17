using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Autonomy;

namespace Nexo.Infrastructure.Certification.HotSwap;

/// <summary>
/// A swap provenance sink that RETAINS events (bounded ring) so the periodic digest can
/// render them, while still logging each one — R2.1's "no unlogged path from objective to
/// runtime" is not traded away for retention.
///
/// <para>Bounded because a long-lived host's loop history is unbounded: when the cap is
/// reached the OLDEST events fall off, and <see cref="DroppedEvents"/> counts what fell —
/// a digest over a saturated ring says "showing the most recent N" rather than silently
/// presenting a truncated history as complete.</para>
/// </summary>
[Experimental(AutonomyExperimental.DiagnosticId, UrlFormat = AutonomyExperimental.UrlFormat)]
public sealed class RecordingBrickSwapProvenanceSink : ICertifiedBrickSwapProvenanceSink
{
    private readonly object _gate = new();
    private readonly Queue<BrickSwapProvenanceEvent> _events = new();
    private readonly int _capacity;
    private readonly ILogger<RecordingBrickSwapProvenanceSink>? _logger;
    private long _dropped;

    /// <summary>Creates the sink. Default capacity holds a long watch history without unbounded growth.</summary>
    public RecordingBrickSwapProvenanceSink(
        ILogger<RecordingBrickSwapProvenanceSink>? logger = null,
        int capacity = 1000)
    {
        if (capacity < 1)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be at least 1.");
        _capacity = capacity;
        _logger = logger;
    }

    /// <summary>Events dropped from the front of the ring since construction.</summary>
    public long DroppedEvents
    {
        get { lock (_gate) { return _dropped; } }
    }

    /// <inheritdoc />
    public void Record(BrickSwapProvenanceEvent provenanceEvent)
    {
        ArgumentNullException.ThrowIfNull(provenanceEvent);

        _logger?.LogInformation(
            "brick-swap provenance: {Outcome} brick {BrickId} generation {Generation} {Reason}",
            provenanceEvent.Outcome, provenanceEvent.BrickId, provenanceEvent.Generation,
            provenanceEvent.Reason ?? "");

        lock (_gate)
        {
            _events.Enqueue(provenanceEvent);
            while (_events.Count > _capacity)
            {
                _events.Dequeue();
                _dropped++;
            }
        }
    }

    /// <summary>The retained events, oldest first.</summary>
    public IReadOnlyList<BrickSwapProvenanceEvent> Snapshot()
    {
        lock (_gate)
        {
            return _events.ToArray();
        }
    }
}
