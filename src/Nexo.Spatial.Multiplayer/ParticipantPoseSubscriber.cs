using Nexo.Spatial.Contracts;

namespace Nexo.Spatial.Multiplayer;

/// <summary>
/// Participant-side read-only scoped pose subscriber.
/// </summary>
public sealed class ParticipantPoseSubscriber
{
    private readonly IMatchScopeStore _scopeStore;
    private readonly IScopedPoseTransport _transport;

    public ParticipantPoseSubscriber(IMatchScopeStore scopeStore, IScopedPoseTransport transport)
    {
        _scopeStore = scopeStore ?? throw new ArgumentNullException(nameof(scopeStore));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    public MatchScopeSnapshotResult TryGetScopeSnapshot(string scopeId, string participantId)
    {
        var scope = _scopeStore.GetScope(scopeId);
        if (scope is null)
            return MatchScopeSnapshotResult.Rejected("scope-not-found", $"Scope '{scopeId}' does not exist.");

        if (!scope.MemberParticipantIds.Any(id => string.Equals(id, participantId, StringComparison.Ordinal)))
        {
            return MatchScopeSnapshotResult.Rejected(
                "not-scope-member",
                $"Participant '{participantId}' is not a member of scope '{scopeId}'.");
        }

        return MatchScopeSnapshotResult.Success(new MatchScopeSnapshot(
            scope.ScopeId,
            scope.HostParticipantId,
            scope.MemberParticipantIds,
            scope.ScopedAtomIds));
    }

    public SubscribeResult TrySubscribe(string scopeId, string participantId)
    {
        var scope = _scopeStore.GetScope(scopeId);
        if (scope is null)
            return SubscribeResult.Rejected("scope-not-found", $"Scope '{scopeId}' does not exist.");

        if (!scope.MemberParticipantIds.Any(id => string.Equals(id, participantId, StringComparison.Ordinal)))
        {
            return SubscribeResult.Rejected(
                "not-scope-member",
                $"Participant '{participantId}' is not a member of scope '{scopeId}'.");
        }

        return SubscribeResult.Success(_transport.Subscribe(scopeId, participantId));
    }
}

/// <summary>
/// Scope snapshot for late joiners — membership and scoped atoms only, no historical poses.
/// </summary>
public sealed record MatchScopeSnapshot(
    string ScopeId,
    string HostParticipantId,
    IReadOnlyCollection<string> MemberParticipantIds,
    IReadOnlyCollection<string> ScopedAtomIds);

public sealed class MatchScopeSnapshotResult
{
    public MatchScopeSnapshotResult(bool succeeded, MatchScopeSnapshot? snapshot, string? rejectionCode, string? reason)
    {
        Succeeded = succeeded;
        Snapshot = snapshot;
        RejectionCode = rejectionCode?.Trim();
        Reason = reason?.Trim();
    }

    public bool Succeeded { get; }

    public MatchScopeSnapshot? Snapshot { get; }

    public string? RejectionCode { get; }

    public string? Reason { get; }

    public static MatchScopeSnapshotResult Success(MatchScopeSnapshot snapshot) => new(true, snapshot, null, null);

    public static MatchScopeSnapshotResult Rejected(string rejectionCode, string reason) =>
        new(false, null, rejectionCode, reason);
}

public sealed class SubscribeResult
{
    public SubscribeResult(bool succeeded, IObservable<ScopedPoseMessage>? stream, string? rejectionCode, string? reason)
    {
        Succeeded = succeeded;
        Stream = stream;
        RejectionCode = rejectionCode?.Trim();
        Reason = reason?.Trim();
    }

    public bool Succeeded { get; }

    public IObservable<ScopedPoseMessage>? Stream { get; }

    public string? RejectionCode { get; }

    public string? Reason { get; }

    public static SubscribeResult Success(IObservable<ScopedPoseMessage> stream) => new(true, stream, null, null);

    public static SubscribeResult Rejected(string rejectionCode, string reason) =>
        new(false, null, rejectionCode, reason);
}
