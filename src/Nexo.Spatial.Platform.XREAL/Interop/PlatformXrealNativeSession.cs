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

    /// <summary>Whether the NRSDK session is active and tracking is available.</summary>
    public bool IsActive => _isActive && XrealSpatialAvailability.IsSupported() && !_hostDisconnected;

    /// <summary>Whether the tethered Android host lost connection to the glasses.</summary>
    public bool IsHostDisconnected => _hostDisconnected;

    /// <summary>Called by the host when the NRSDK session starts or stops.</summary>
    public void SetActive(bool active) => _isActive = active;

    /// <summary>Called by the host when the glasses disconnect from the tethered device.</summary>
    public void SetHostDisconnected(bool disconnected) => _hostDisconnected = disconnected;

    /// <summary>Reads the latest native pose frame for an anchor, if available.</summary>
    public XrealNativePoseFrame? TryGetAnchorPose(string atomId)
    {
        if (!IsActive || string.IsNullOrWhiteSpace(atomId))
            return null;

        return XrealNativeBridge.TryGetAnchorPose(atomId);
    }

    /// <summary>Observes native pose frames for an anchor id.</summary>
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
