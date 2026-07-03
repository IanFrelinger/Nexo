using System.Reactive.Linq;

namespace Nexo.Spatial.Platform.XREAL.Interop;

/// <summary>
/// Production NRSDK session adapter routed through <see cref="XrealNativeBridge"/>.
/// </summary>
public sealed class PlatformXrealNativeSession : IXrealNativeSession
{
    private volatile bool _isActive;
    private volatile bool _hostDisconnected;

    public PlatformXrealNativeSession(bool isActive = false) => _isActive = isActive;

    public bool IsActive => _isActive && XrealSpatialAvailability.IsSupported() && !_hostDisconnected;

    public bool IsHostDisconnected => _hostDisconnected;

    public void SetActive(bool active) => _isActive = active;

    public void SetHostDisconnected(bool disconnected) => _hostDisconnected = disconnected;

    public XrealNativePoseFrame? TryGetAnchorPose(string atomId)
    {
        if (!IsActive || string.IsNullOrWhiteSpace(atomId))
            return null;

        return XrealNativeBridge.TryGetAnchorPose(atomId);
    }

    public IObservable<XrealNativePoseFrame> ObserveAnchorPose(string atomId)
    {
        if (string.IsNullOrWhiteSpace(atomId) || !_isActive || !XrealSpatialAvailability.IsSupported())
            return Observable.Empty<XrealNativePoseFrame>();

        if (_hostDisconnected)
        {
            return Observable.Return(new XrealNativePoseFrame(
                0, 0, 0, 0, 0, 0, 1,
                XrealNativeTrackingQuality.Lost,
                DateTimeOffset.UtcNow));
        }

        return XrealNativeBridge.ObserveAnchorPose(atomId);
    }
}
