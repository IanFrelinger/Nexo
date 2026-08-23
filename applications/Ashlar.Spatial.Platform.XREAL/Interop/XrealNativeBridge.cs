using System.Reactive.Linq;

namespace Ashlar.Spatial.Platform.XREAL.Interop;

/// <summary>
/// Native NRSDK entry points. Headless CI always receives null/empty (fail-closed).
/// </summary>
internal static class XrealNativeBridge
{
    internal static XrealNativePoseFrame? TryGetAnchorPose(string atomId)
    {
        if (!XrealSpatialAvailability.IsSupported())
            return null;

        return null;
    }

    internal static IObservable<XrealNativePoseFrame> ObserveAnchorPose(string atomId)
    {
        if (!XrealSpatialAvailability.IsSupported())
            return Observable.Empty<XrealNativePoseFrame>();

        return Observable.Empty<XrealNativePoseFrame>();
    }
}
