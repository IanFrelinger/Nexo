using System.Reactive.Linq;

namespace Nexo.Spatial.Platform.VisionPro.Interop;

/// <summary>
/// Production visionOS session adapter routed through <see cref="VisionProNativeBridge"/>.
/// </summary>
public sealed class PlatformVisionProNativeSession : IVisionProNativeSession
{
    private volatile bool _isActive;
    private volatile bool _immersiveSpaceActive;
    private volatile bool _interrupted;

    public PlatformVisionProNativeSession(bool isActive = false, bool immersiveSpaceActive = false)
    {
        _isActive = isActive;
        _immersiveSpaceActive = immersiveSpaceActive;
    }

    /// <summary>Whether WorldTracking is active with an open immersive space and no interruption.</summary>
    public bool IsActive =>
        _isActive
        && _immersiveSpaceActive
        && VisionProSpatialAvailability.IsSupported()
        && !_interrupted;

    /// <summary>Whether the host has an active immersive space.</summary>
    public bool IsImmersiveSpaceActive => _immersiveSpaceActive;

    /// <summary>Whether visionOS reported a tracking interruption.</summary>
    public bool IsInterrupted => _interrupted;

    /// <summary>Called by the host when WorldTracking session starts or stops.</summary>
    public void SetActive(bool active) => _isActive = active;

    /// <summary>Called by the host when an immersive space opens or closes.</summary>
    public void SetImmersiveSpaceActive(bool active) => _immersiveSpaceActive = active;

    /// <summary>Called by the host on visionOS tracking interruption callbacks.</summary>
    public void SetInterrupted(bool interrupted) => _interrupted = interrupted;

    /// <summary>Reads the latest native pose frame for an anchor, if available.</summary>
    public VisionProNativePoseFrame? TryGetAnchorPose(string atomId)
    {
        if (!IsActive || string.IsNullOrWhiteSpace(atomId))
            return null;

        return VisionProNativeBridge.TryGetAnchorPose(atomId);
    }

    /// <summary>Observes native pose frames for an anchor id.</summary>
    public IObservable<VisionProNativePoseFrame> ObserveAnchorPose(string atomId)
    {
        if (string.IsNullOrWhiteSpace(atomId) || !_isActive || !VisionProSpatialAvailability.IsSupported())
            return Observable.Empty<VisionProNativePoseFrame>();

        if (!_immersiveSpaceActive)
            return Observable.Empty<VisionProNativePoseFrame>();

        if (_interrupted)
        {
            return Observable.Return(new VisionProNativePoseFrame(
                0, 0, 0, 0, 0, 0, 1,
                VisionProNativeTrackingQuality.NotAvailable,
                DateTimeOffset.UtcNow));
        }

        return VisionProNativeBridge.ObserveAnchorPose(atomId);
    }
}
