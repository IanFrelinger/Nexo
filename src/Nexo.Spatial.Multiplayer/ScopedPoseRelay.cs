namespace Nexo.Spatial.Multiplayer;

/// <summary>
/// Host-authoritative scoped pose relay coordinating publisher and subscriber paths.
/// Consumes opaque certified atom IDs — identity resolution stays upstream of this project.
/// </summary>
public sealed class ScopedPoseRelay
{
    private readonly IMatchScopeStore _scopeStore;
    private readonly HostPosePublisher _publisher;
    private readonly ParticipantPoseSubscriber _subscriber;

    public ScopedPoseRelay(
        IMatchScopeStore scopeStore,
        IScopedPoseTransport transport)
    {
        _scopeStore = scopeStore ?? throw new ArgumentNullException(nameof(scopeStore));
        _publisher = new HostPosePublisher(scopeStore, transport);
        _subscriber = new ParticipantPoseSubscriber(scopeStore, transport);
    }

    /// <summary>Host-side publisher for scoped atom poses.</summary>
    public HostPosePublisher Host => _publisher;

    /// <summary>Participant-side subscriber for scoped atom poses.</summary>
    public ParticipantPoseSubscriber Participants => _subscriber;

    /// <summary>Match scope store backing this relay.</summary>
    public IMatchScopeStore ScopeStore => _scopeStore;
}
