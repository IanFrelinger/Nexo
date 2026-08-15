using Nexo.Spatial.Contracts;

namespace Nexo.Spatial.Multiplayer;

/// <summary>
/// Host-side publisher for scoped atom poses. Only the scope host may publish.
/// </summary>
public sealed class HostPosePublisher
{
    private readonly IMatchScopeStore _scopeStore;
    private readonly IScopedPoseTransport _transport;

    public HostPosePublisher(IMatchScopeStore scopeStore, IScopedPoseTransport transport)
    {
        _scopeStore = scopeStore ?? throw new ArgumentNullException(nameof(scopeStore));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    /// <summary>Publishes a scoped pose update when the caller is the scope host.</summary>
    public PoseRelayResult TryPublishPose(
        string scopeId,
        string publisherParticipantId,
        string atomId,
        PoseSample pose)
    {
        var scope = _scopeStore.GetScope(scopeId);
        if (scope is null)
            return PoseRelayResult.Rejected("scope-not-found", $"Scope '{scopeId}' does not exist.");

        if (!string.Equals(scope.HostParticipantId, publisherParticipantId, StringComparison.Ordinal))
        {
            return PoseRelayResult.Rejected(
                "not-scope-host",
                $"Participant '{publisherParticipantId}' is not the host for scope '{scopeId}'.");
        }

        if (!scope.ScopedAtomIds.Any(id => string.Equals(id, atomId, StringComparison.Ordinal)))
        {
            return PoseRelayResult.Rejected(
                "atom-not-in-scope",
                $"Atom '{atomId}' is not in scope '{scopeId}'.");
        }

        return _transport.TryPublish(scopeId, publisherParticipantId, atomId, pose);
    }

    /// <summary>Broadcasts a host-lost signal for an atom within a scope.</summary>
    public void PublishHostLost(string scopeId, string atomId, string reason)
    {
        if (_transport is InMemoryScopedPoseTransport inMemory)
            inMemory.PublishSignal(scopeId, atomId, reason);
    }
}
