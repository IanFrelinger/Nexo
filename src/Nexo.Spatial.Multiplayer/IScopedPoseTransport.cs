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

/// <summary>
/// Pose message relayed within a match scope.
/// </summary>
public sealed record ScopedPoseMessage(
    string ScopeId,
    string AtomId,
    PoseSample? Pose,
    bool Lost,
    string? SignalReason);

/// <summary>
/// Result of attempting to publish a scoped pose.
/// </summary>
public sealed class PoseRelayResult
{
    public PoseRelayResult(bool accepted, string? rejectionCode, string? reason)
    {
        Accepted = accepted;
        RejectionCode = rejectionCode?.Trim();
        Reason = reason?.Trim();
    }

    public bool Accepted { get; }

    public string? RejectionCode { get; }

    public string? Reason { get; }

    public static PoseRelayResult AcceptedResult() => new(true, null, null);

    public static PoseRelayResult Rejected(string rejectionCode, string reason) =>
        new(false, rejectionCode, reason);
}
