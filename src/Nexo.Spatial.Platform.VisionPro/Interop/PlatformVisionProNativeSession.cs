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

    public bool IsActive =>
        _isActive
        && _immersiveSpaceActive
        && VisionProSpatialAvailability.IsSupported()
        && !_interrupted;

    public bool IsImmersiveSpaceActive => _immersiveSpaceActive;

    public bool IsInterrupted => _interrupted;

    public void SetActive(bool active) => _isActive = active;

    public void SetImmersiveSpaceActive(bool active) => _immersiveSpaceActive = active;

    public void SetInterrupted(bool interrupted) => _interrupted = interrupted;

    public VisionProNativePoseFrame? TryGetAnchorPose(string atomId)
    {
        if (!IsActive || string.IsNullOrWhiteSpace(atomId))
            return null;

        return VisionProNativeBridge.TryGetAnchorPose(atomId);
    }

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
