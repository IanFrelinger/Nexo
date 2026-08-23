using System.Reactive.Linq;
using Ashlar.Spatial.Contracts;
using Ashlar.Spatial.Platform.XREAL.Interop;

namespace Ashlar.Spatial.Platform.XREAL;

/// <summary>
/// XREAL NRSDK platform <see cref="ISpatialAnchorProvider"/> (P2-S2).
/// </summary>
/// <remarks>
/// Targets NRSDK on a tethered Android host with 6DoF world-locked tracking.
/// Glasses are not self-contained — pose originates from the phone-side NRSDK session.
/// Providers emit raw poses only; filtering lives in <see cref="PoseStreamConsumer"/>.
/// </remarks>
public sealed class XrealSpatialAnchorProvider : ISpatialAnchorProvider
{
    private readonly IXrealNativeSession _session;

    public XrealSpatialAnchorProvider()
        : this(UninitializedXrealNativeSession.Instance)
    {
    }

    public XrealSpatialAnchorProvider(IXrealNativeSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    /// <summary>Convenience factory for hosts that manage <see cref="PlatformXrealNativeSession"/> lifecycle inline.</summary>
    public static XrealSpatialAnchorProvider WithPlatformSession(bool sessionActive = false) =>
        new(new PlatformXrealNativeSession(sessionActive));

    /// <summary>Returns the latest pose for an atom, or <see langword="null"/> when unavailable.</summary>
    public Task<PoseSample?> GetCurrentPose(string atomId)
    {
        if (string.IsNullOrWhiteSpace(atomId) || !IsTrackingAvailable())
            return Task.FromResult<PoseSample?>(null);

        var frame = _session.TryGetAnchorPose(atomId);
        if (frame is null)
            return Task.FromResult<PoseSample?>(null);

        return Task.FromResult<PoseSample?>(XrealTrackingStateMapper.ToPoseSample(frame));
    }

    /// <summary>Observes pose updates for an atom; emits lost samples when tracking is unavailable.</summary>
    public IObservable<PoseSample> ObservePose(string atomId)
    {
        if (string.IsNullOrWhiteSpace(atomId) || !IsTrackingAvailable())
            return Observable.Return(CreateLostSample());

        if (_session.IsHostDisconnected)
            return Observable.Return(CreateLostSample());

        var stream = _session.ObserveAnchorPose(atomId)
            .Select(XrealTrackingStateMapper.ToPoseSample);

        var initial = _session.TryGetAnchorPose(atomId);
        if (initial is not null)
            stream = stream.StartWith(XrealTrackingStateMapper.ToPoseSample(initial));

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
