namespace Nexo.BackgroundAgents.Observations;

/// <summary>
/// Append-only, multi-reader store for <see cref="RuntimeObservation"/> rows.
///
/// <para>Designed for the planner ↔ tester ↔ optimizer feedback loop: any agent
/// (or any tool the planner invokes) can <see cref="Append"/> a fact, and the
/// next planner cycle reads the recent tail back via <see cref="ReadSince"/>
/// to populate its <c>WorldSnapshot.RecentObservations</c>.</para>
///
/// <para>Implementations MUST be safe to call from multiple threads.
/// Implementations SHOULD be best-effort with respect to I/O failure: a failed
/// <see cref="Append"/> is logged and swallowed rather than propagated, because
/// telemetry must never destabilise an agent cycle.</para>
/// </summary>
public interface IObservationStore
{
    /// <summary>
    /// Append a single observation. Best-effort — implementations should swallow
    /// I/O exceptions rather than throw.
    /// </summary>
    void Append(RuntimeObservation observation);

    /// <summary>
    /// Read observations from the store, optionally filtered by timestamp and kind.
    /// Returned in append order (oldest first); callers that want the most recent
    /// N events should take the tail.
    /// </summary>
    /// <param name="since">If set, only observations with <c>ts &gt;= since</c> are returned.</param>
    /// <param name="kind">If set, only observations of this kind are returned.</param>
    /// <param name="limit">If set, at most this many observations are returned (oldest first).</param>
    IEnumerable<RuntimeObservation> ReadSince(
        DateTimeOffset? since = null,
        ObservationKind? kind = null,
        int? limit = null);

    /// <summary>
    /// Path the store writes to (or a synthetic identifier for in-memory stores).
    /// Exposed for diagnostics and CLI commands.
    /// </summary>
    string Location { get; }
}

