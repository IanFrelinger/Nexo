using Nexo.Spatial.Contracts;
using Nexo.Spatial.Platform.XREAL.Interop;

namespace Nexo.Spatial.Platform.XREAL;

/// <summary>Xreal tracking state mapper.</summary>
internal static class XrealTrackingStateMapper
{
    /// <summary>To pose sample.</summary>
    /// <param name="frame">Frame.</param>
    internal static PoseSample ToPoseSample(XrealNativePoseFrame frame) =>
        new(
            new SpatialVector3(frame.PositionX, frame.PositionY, frame.PositionZ),
            new SpatialQuaternion(frame.RotationX, frame.RotationY, frame.RotationZ, frame.RotationW),
            MapConfidence(frame.TrackingQuality),
            frame.Timestamp,
            MapTrackingState(frame.TrackingQuality));

    /// <summary>Map tracking state.</summary>
    /// <param name="quality">Quality.</param>
    internal static TrackingState MapTrackingState(XrealNativeTrackingQuality quality) =>
        quality switch
        {
            XrealNativeTrackingQuality.Tracking => TrackingState.Tracking,
            XrealNativeTrackingQuality.Limited => TrackingState.Occluded,
            _ => TrackingState.Lost
        };

    /// <summary>Map confidence.</summary>
    /// <param name="quality">Quality.</param>
    internal static double MapConfidence(XrealNativeTrackingQuality quality) =>
        quality switch
        {
            XrealNativeTrackingQuality.Tracking => 0.9,
            XrealNativeTrackingQuality.Limited => 0.3,
            _ => 0
        };
}
