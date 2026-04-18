namespace Nexo.BackgroundAgents.Objectives;

/// <summary>
/// Backlog manager for <see cref="ObjectiveDocument"/>s. The store owns the
/// physical layout (which folder maps to which status), the lifecycle
/// transitions (Pending → InProgress → Done | Blocked → Pending), and the
/// concurrency semantics (only one agent ever has a given objective in
/// InProgress at a time).
///
/// <para>The default <see cref="ObjectiveStore"/> implementation is filesystem-
/// backed under <c>.nexo/runtime-studio/objectives/{pending|in_progress|done|blocked}/</c>;
/// implementations are free to back the same contract with a database or in-memory
/// fixture for tests.</para>
/// </summary>
public interface IObjectiveStore
{
    /// <summary>List all objectives, optionally filtered by status.</summary>
    IReadOnlyList<ObjectiveDocument> List(ObjectiveStatus? status = null);

    /// <summary>Look up an objective by id (across all statuses).</summary>
    ObjectiveDocument? Find(string id);

    /// <summary>
    /// Atomically claim the next pending objective for the given owner: moves the
    /// highest-priority pending objective into <c>InProgress</c>, stamps it with
    /// the owner, increments its <c>Attempts</c> counter, and returns the updated
    /// document. Returns <c>null</c> when the pending queue is empty.
    /// </summary>
    ObjectiveDocument? ClaimNext(string owner);

    /// <summary>Persist an entirely new objective into the <c>Pending</c> bucket.</summary>
    ObjectiveDocument Add(ObjectiveDocument document);

    /// <summary>
    /// Mark an in-progress objective as <c>Done</c>. Throws if the objective is
    /// missing or not currently in <c>InProgress</c>.
    /// </summary>
    ObjectiveDocument Complete(string id, string? note = null);

    /// <summary>
    /// Mark an objective as <c>Blocked</c> with the given reason. Allowed from any
    /// non-terminal status (Pending and InProgress).
    /// </summary>
    ObjectiveDocument Block(string id, string reason);

    /// <summary>Move a blocked objective back to <c>Pending</c>.</summary>
    ObjectiveDocument Unblock(string id);

    /// <summary>
    /// Filesystem (or synthetic) root the store reads/writes — exposed for diagnostics
    /// and CLI commands.
    /// </summary>
    string Location { get; }
}
