using System.Reactive.Linq;

namespace Ashlar.Spatial.Platform.VisionPro.Interop;

/// <summary>
/// Native visionOS WorldTracking entry points. Headless CI always receives null/empty (fail-closed).
/// </summary>
internal static class VisionProNativeBridge
{
    internal static VisionProNativePoseFrame? TryGetAnchorPose(string atomId)
    {
        if (!VisionProSpatialAvailability.IsSupported())
            return null;

        return null;
    }

    internal static IObservable<VisionProNativePoseFrame> ObserveAnchorPose(string atomId)
    {
        if (!VisionProSpatialAvailability.IsSupported())
            return Observable.Empty<VisionProNativePoseFrame>();

        return Observable.Empty<VisionProNativePoseFrame>();
    }
}
