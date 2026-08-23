using Ashlar.Spatial.Contracts;

namespace Ashlar.Spatial.Multiplayer;

/// <summary>Result of reading a participant scope snapshot.</summary>
public sealed class MatchScopeSnapshotResult
{
    public MatchScopeSnapshotResult(bool succeeded, MatchScopeSnapshot? snapshot, string? rejectionCode, string? reason)
    {
        Succeeded = succeeded;
        Snapshot = snapshot;
        RejectionCode = rejectionCode?.Trim();
        Reason = reason?.Trim();
    }

    /// <summary>Whether the snapshot was retrieved successfully.</summary>
    public bool Succeeded { get; }

    /// <summary>Scope snapshot when <see cref="Succeeded"/> is true.</summary>
    public MatchScopeSnapshot? Snapshot { get; }

    /// <summary>Machine-readable rejection code when <see cref="Succeeded"/> is false.</summary>
    public string? RejectionCode { get; }

    /// <summary>Human-readable rejection detail when <see cref="Succeeded"/> is false.</summary>
    public string? Reason { get; }

    /// <summary>Creates a successful snapshot result.</summary>
    public static MatchScopeSnapshotResult Success(MatchScopeSnapshot snapshot) => new(true, snapshot, null, null);

    /// <summary>Creates a rejected snapshot result.</summary>
    public static MatchScopeSnapshotResult Rejected(string rejectionCode, string reason) =>
        new(false, null, rejectionCode, reason);
}
