using System.Reactive.Linq;

namespace Nexo.Spatial.Platform.ARKit.Interop;

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

    public bool IsActive => _isActive && ArKitSpatialAvailability.IsSupported() && !_isInterrupted;

    public bool IsInterrupted => _isInterrupted;

    /// <summary>Called by the host when ARKit <c>ARSession</c> run configuration starts.</summary>
    public void SetActive(bool active) => _isActive = active;

    /// <summary>Called by the host on ARKit session-interruption callbacks.</summary>
    public void SetInterrupted(bool interrupted) => _isInterrupted = interrupted;

    public ArKitNativePoseFrame? TryGetAnchorPose(string atomId)
    {
        if (!IsActive || string.IsNullOrWhiteSpace(atomId))
            return null;

        return ArKitNativeBridge.TryGetAnchorPose(atomId);
    }

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
