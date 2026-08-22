using System.Reactive.Linq;
using Ashlar.Spatial.Contracts;
using Ashlar.Spatial.Platform.VisionPro.Interop;

namespace Ashlar.Spatial.Platform.VisionPro;

/// <summary>
/// visionOS WorldTracking platform <see cref="ISpatialAnchorProvider"/> (P2-S3).
/// </summary>
/// <remarks>
/// Architecturally ARKit-on-visionOS, but packaged separately from
/// <c>Ashlar.Spatial.Platform.ARKit</c> for visionOS consumer isolation and immersive-space lifecycle gating.
/// Providers emit raw poses only; filtering lives in <see cref="PoseStreamConsumer"/>.
/// </remarks>
public sealed class VisionProSpatialAnchorProvider : ISpatialAnchorProvider
{
    private readonly IVisionProNativeSession _session;

    public VisionProSpatialAnchorProvider()
        : this(UninitializedVisionProNativeSession.Instance)
    {
    }

    public VisionProSpatialAnchorProvider(IVisionProNativeSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    /// <summary>Convenience factory for hosts that manage <see cref="PlatformVisionProNativeSession"/> lifecycle inline.</summary>
    public static VisionProSpatialAnchorProvider WithPlatformSession(
        bool sessionActive = false,
        bool immersiveSpaceActive = false) =>
        new(new PlatformVisionProNativeSession(sessionActive, immersiveSpaceActive));

    /// <summary>Returns the latest pose for an atom, or <see langword="null"/> when unavailable.</summary>
    public Task<PoseSample?> GetCurrentPose(string atomId)
    {
        if (string.IsNullOrWhiteSpace(atomId) || !IsTrackingAvailable())
            return Task.FromResult<PoseSample?>(null);

        var frame = _session.TryGetAnchorPose(atomId);
        if (frame is null)
            return Task.FromResult<PoseSample?>(null);

        return Task.FromResult<PoseSample?>(VisionProTrackingStateMapper.ToPoseSample(frame));
    }

    /// <summary>Observes pose updates for an atom; emits lost samples when tracking is unavailable.</summary>
    public IObservable<PoseSample> ObservePose(string atomId)
    {
        if (string.IsNullOrWhiteSpace(atomId) || !IsTrackingAvailable())
            return Observable.Return(CreateLostSample());

        if (_session.IsInterrupted)
            return Observable.Return(CreateLostSample());

        var stream = _session.ObserveAnchorPose(atomId)
            .Select(VisionProTrackingStateMapper.ToPoseSample);

        var initial = _session.TryGetAnchorPose(atomId);
        if (initial is not null)
            stream = stream.StartWith(VisionProTrackingStateMapper.ToPoseSample(initial));

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
