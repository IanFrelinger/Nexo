using System.Reactive.Linq;
using Nexo.Spatial.Contracts;
using Nexo.Spatial.Platform.ARKit.Interop;

namespace Nexo.Spatial.Platform.ARKit;

/// <summary>
/// ARKit platform <see cref="ISpatialAnchorProvider"/> (P2-S1).
/// Reads raw poses from an injected <see cref="IArKitNativeSession"/>; non-iOS and inactive sessions fail closed.
/// </summary>
/// <remarks>
/// Session start/stop is owned by the host application — inject a session whose <see cref="IArKitNativeSession.IsActive"/>
/// reflects the live ARKit <c>ARSession</c>. This provider emits raw poses only; filtering lives in
/// <see cref="PoseStreamConsumer"/>.
/// </remarks>
public sealed class ArKitSpatialAnchorProvider : ISpatialAnchorProvider
{
    private readonly IArKitNativeSession _session;

    /// <summary>Creates a provider with no active native session (fail-closed).</summary>
    public ArKitSpatialAnchorProvider()
        : this(UninitializedArKitNativeSession.Instance)
    {
    }

    /// <summary>Creates a provider bound to a host-managed ARKit session.</summary>
    public ArKitSpatialAnchorProvider(IArKitNativeSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    /// <summary>
    /// Convenience factory for hosts that manage <see cref="PlatformArKitNativeSession"/> lifecycle inline.
    /// </summary>
    public static ArKitSpatialAnchorProvider WithPlatformSession(bool sessionActive = false) =>
        new(new PlatformArKitNativeSession(sessionActive));

    public Task<PoseSample?> GetCurrentPose(string atomId)
    {
        if (string.IsNullOrWhiteSpace(atomId) || !IsTrackingAvailable())
            return Task.FromResult<PoseSample?>(null);

        var frame = _session.TryGetAnchorPose(atomId);
        if (frame is null)
            return Task.FromResult<PoseSample?>(null);

        return Task.FromResult<PoseSample?>(ArKitTrackingStateMapper.ToPoseSample(frame));
    }

    public IObservable<PoseSample> ObservePose(string atomId)
    {
        if (string.IsNullOrWhiteSpace(atomId) || !IsTrackingAvailable())
            return Observable.Return(CreateLostSample());

        if (_session.IsInterrupted)
            return Observable.Return(CreateLostSample());

        var stream = _session.ObserveAnchorPose(atomId)
            .Select(ArKitTrackingStateMapper.ToPoseSample);

        var initial = _session.TryGetAnchorPose(atomId);
        if (initial is not null)
            stream = stream.StartWith(ArKitTrackingStateMapper.ToPoseSample(initial));

        return stream.DefaultIfEmpty(CreateLostSample());
    }

    private bool IsTrackingAvailable() => _session.IsActive;

    private static PoseSample CreateLostSample() =>
        new(
            new SpatialVector3(0, 0, 0),
            new SpatialQuaternion(0, 0, 0, 1),
            0,
            DateTimeOffset.UtcNow,
            TrackingState.Lost);
}
