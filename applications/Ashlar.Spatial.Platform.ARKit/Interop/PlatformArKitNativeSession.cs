using System.Reactive.Linq;

namespace Ashlar.Spatial.Platform.ARKit.Interop;

/// <summary>
/// Production ARKit session adapter routed through <see cref="ArKitNativeBridge"/>.
/// Active only on iOS 11+ when the host sets <see cref="IsActive"/>.
/// </summary>
public sealed class PlatformArKitNativeSession : IArKitNativeSession
{
    private volatile bool _isActive;
    private volatile bool _isInterrupted;

    /// <summary>
    /// Creates a session handle. The host must call <see cref="SetActive"/> after starting ARKit.
    /// </summary>
    public PlatformArKitNativeSession(bool isActive = false)
    {
        _isActive = isActive;
    }

    /// <summary>Whether the ARKit session is active and tracking is available.</summary>
    public bool IsActive => _isActive && ArKitSpatialAvailability.IsSupported() && !_isInterrupted;

    /// <summary>Whether ARKit reported a session interruption.</summary>
    public bool IsInterrupted => _isInterrupted;

    /// <summary>Called by the host when ARKit <c>ARSession</c> run configuration starts.</summary>
    public void SetActive(bool active) => _isActive = active;

    /// <summary>Called by the host on ARKit session-interruption callbacks.</summary>
    public void SetInterrupted(bool interrupted) => _isInterrupted = interrupted;

    /// <summary>Reads the latest native pose frame for an anchor, if available.</summary>
    public ArKitNativePoseFrame? TryGetAnchorPose(string atomId)
    {
        if (!IsActive || string.IsNullOrWhiteSpace(atomId))
            return null;

        return ArKitNativeBridge.TryGetAnchorPose(atomId);
    }

    /// <summary>Observes native pose frames for an anchor id.</summary>
    public IObservable<ArKitNativePoseFrame> ObserveAnchorPose(string atomId)
    {
        if (string.IsNullOrWhiteSpace(atomId) || !_isActive || !ArKitSpatialAvailability.IsSupported())
            return Observable.Empty<ArKitNativePoseFrame>();

        if (_isInterrupted)
        {
            return Observable.Return(new ArKitNativePoseFrame(
                0, 0, 0, 0, 0, 0, 1,
                ArKitNativeTrackingQuality.NotAvailable,
                DateTimeOffset.UtcNow));
        }

        return ArKitNativeBridge.ObserveAnchorPose(atomId);
    }
}
