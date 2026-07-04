using Nexo.Spatial.Contracts;
using Nexo.Spatial.Platform.VisionPro.Interop;

namespace Nexo.Spatial.Platform.VisionPro;

/// <summary>Vision pro tracking state mapper.</summary>
internal static class VisionProTrackingStateMapper
{
    /// <summary>To pose sample.</summary>
    /// <param name="frame">Frame.</param>
    internal static PoseSample ToPoseSample(VisionProNativePoseFrame frame) =>
        new(
            new SpatialVector3(frame.PositionX, frame.PositionY, frame.PositionZ),
            new SpatialQuaternion(frame.RotationX, frame.RotationY, frame.RotationZ, frame.RotationW),
            MapConfidence(frame.TrackingQuality),
            frame.Timestamp,
            MapTrackingState(frame.TrackingQuality));

    /// <summary>Map tracking state.</summary>
    /// <param name="quality">Quality.</param>
    internal static TrackingState MapTrackingState(VisionProNativeTrackingQuality quality) =>
        quality switch
        {
            VisionProNativeTrackingQuality.Normal => TrackingState.Tracking,
            VisionProNativeTrackingQuality.Limited => TrackingState.Occluded,
            _ => TrackingState.Lost
        };

    /// <summary>Map confidence.</summary>
    /// <param name="quality">Quality.</param>
    internal static double MapConfidence(VisionProNativeTrackingQuality quality) =>
        quality switch
        {
            VisionProNativeTrackingQuality.Normal => 0.95,
            VisionProNativeTrackingQuality.Limited => 0.25,
            _ => 0
        };
}
