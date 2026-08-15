using Nexo.Spatial.Contracts;

namespace Nexo.Spatial.Multiplayer;

/// <summary>
/// In-memory transport double for scoped pose relay tests.
/// </summary>
public interface IScopedPoseTransport
{
    PoseRelayResult TryPublish(string scopeId, string publisherParticipantId, string atomId, PoseSample pose);

    IObservable<ScopedPoseMessage> Subscribe(string scopeId, string subscriberParticipantId);
}
