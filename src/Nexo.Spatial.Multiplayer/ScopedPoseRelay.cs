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

    public HostPosePublisher Host => _publisher;

    public ParticipantPoseSubscriber Participants => _subscriber;

    public IMatchScopeStore ScopeStore => _scopeStore;
}
