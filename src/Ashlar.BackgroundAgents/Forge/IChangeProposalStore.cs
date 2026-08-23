namespace Ashlar.BackgroundAgents.Forge;

/// <summary>
/// Lifecycle manager for <see cref="ChangeProposal"/>s. The default
/// <see cref="ChangeProposalStore"/> implementation is filesystem-backed under
/// <c>.ashlar/runtime-studio/forge/{proposed|approved|rejected|applied|stale}/</c>;
/// implementations are free to back the same contract with a database or
/// in-memory fixture for tests.
/// </summary>
public interface IChangeProposalStore
{
    /// <summary>List proposals, optionally filtered by status.</summary>
    IReadOnlyList<ChangeProposal> List(ChangeProposalStatus? status = null);

    /// <summary>Look up a proposal by id (across all statuses).</summary>
    ChangeProposal? Find(string id);

    /// <summary>Persist a brand-new proposal in the <c>Proposed</c> bucket.</summary>
    ChangeProposal Add(ChangeProposal proposal);

    /// <summary>Move a proposed proposal to <c>Approved</c>.</summary>
    ChangeProposal Approve(string id, string? approver = null, string? note = null);

    /// <summary>Move a proposed proposal to <c>Rejected</c>.</summary>
    ChangeProposal Reject(string id, string? reviewer = null, string? note = null);

    /// <summary>Move an approved proposal to <c>Applied</c>.</summary>
    ChangeProposal MarkApplied(string id, string? note = null);

    /// <summary>Move a proposal to <c>Stale</c> (e.g. expired by janitor).</summary>
    ChangeProposal MarkStale(string id, string? note = null);

    /// <summary>Filesystem (or synthetic) root the store reads/writes — exposed for diagnostics.</summary>
    string Location { get; }
}
