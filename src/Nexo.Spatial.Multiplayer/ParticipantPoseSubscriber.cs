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

    /// <summary>Reads a participant-visible snapshot of scope membership and atoms.</summary>
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

    /// <summary>Subscribes a scope member to scoped pose updates.</summary>
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
