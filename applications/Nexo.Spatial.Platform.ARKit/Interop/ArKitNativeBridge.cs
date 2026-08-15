using System.Reactive.Linq;

namespace Nexo.Spatial.Platform.ARKit.Interop;

/// <summary>
/// Native ARKit entry points. On iOS hosts with an active session, a future device adapter binds these methods
/// to <c>ARSession</c>/<c>ARAnchor</c> APIs. Headless CI always receives null/empty (fail-closed).
/// </summary>
internal static class ArKitNativeBridge
{
    internal static ArKitNativePoseFrame? TryGetAnchorPose(string atomId)
    {
        if (!ArKitSpatialAvailability.IsSupported())
            return null;

        // Native ARKit anchor transform lookup binds here on iOS device hosts.
        return null;
    }

    internal static IObservable<ArKitNativePoseFrame> ObserveAnchorPose(string atomId)
    {
        if (!ArKitSpatialAvailability.IsSupported())
            return Observable.Empty<ArKitNativePoseFrame>();

        // Native ARKit frame delegate binds here on iOS device hosts.
        return Observable.Empty<ArKitNativePoseFrame>();
    }
}
