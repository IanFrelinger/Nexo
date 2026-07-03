using System.Reactive.Linq;
using Nexo.Spatial.Contracts;

namespace Nexo.Spatial.Platform.ARKit;

/// <summary>
/// ARKit platform <see cref="ISpatialAnchorProvider"/> (P2).
/// Until native ARKit session wiring exists, non-iOS and uninitialized sessions fail closed.
/// </summary>
public sealed class ArKitSpatialAnchorProvider : ISpatialAnchorProvider
{
    private readonly bool _sessionReady;

    /// <summary>
    /// Creates a provider. On non-iOS hosts, poses are never emitted.
    /// </summary>
    /// <param name="sessionReady">When true on iOS, indicates native session is active (future P2 wiring).</param>
    public ArKitSpatialAnchorProvider(bool sessionReady = false)
    {
        _sessionReady = sessionReady;
    }

    public Task<PoseSample?> GetCurrentPose(string atomId)
    {
        if (string.IsNullOrWhiteSpace(atomId))
            return Task.FromResult<PoseSample?>(null);

        if (!IsTrackingAvailable())
            return Task.FromResult<PoseSample?>(null);

        // Native ARKit pose lookup is P2 follow-up; unavailable until wired.
        return Task.FromResult<PoseSample?>(null);
    }

    public IObservable<PoseSample> ObservePose(string atomId)
    {
        if (string.IsNullOrWhiteSpace(atomId) || !IsTrackingAvailable())
            return Observable.Return(CreateLostSample());

        // Session ready on iOS but native bridge not wired yet — still fail closed.
        return Observable.Return(CreateLostSample());
    }

    private bool IsTrackingAvailable() =>
        ArKitSpatialAvailability.IsSupported() && _sessionReady;

    private static PoseSample CreateLostSample() =>
        new(
            new SpatialVector3(0, 0, 0),
            new SpatialQuaternion(0, 0, 0, 1),
            0,
            DateTimeOffset.UtcNow,
            TrackingState.Lost);
}
