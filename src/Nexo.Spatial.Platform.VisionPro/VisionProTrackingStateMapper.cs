using Nexo.Spatial.Contracts;
using Nexo.Spatial.Platform.VisionPro.Interop;

namespace Nexo.Spatial.Platform.VisionPro;

internal static class VisionProTrackingStateMapper
{
    internal static PoseSample ToPoseSample(VisionProNativePoseFrame frame) =>
        new(
            new SpatialVector3(frame.PositionX, frame.PositionY, frame.PositionZ),
            new SpatialQuaternion(frame.RotationX, frame.RotationY, frame.RotationZ, frame.RotationW),
            MapConfidence(frame.TrackingQuality),
            frame.Timestamp,
            MapTrackingState(frame.TrackingQuality));

    internal static TrackingState MapTrackingState(VisionProNativeTrackingQuality quality) =>
        quality switch
        {
            VisionProNativeTrackingQuality.Normal => TrackingState.Tracking,
            VisionProNativeTrackingQuality.Limited => TrackingState.Occluded,
            _ => TrackingState.Lost
        };

    internal static double MapConfidence(VisionProNativeTrackingQuality quality) =>
        quality switch
        {
            VisionProNativeTrackingQuality.Normal => 0.95,
            VisionProNativeTrackingQuality.Limited => 0.25,
            _ => 0
        };
}
