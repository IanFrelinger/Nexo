using Nexo.Spatial.Contracts;
using Nexo.Spatial.Platform.ARKit.Interop;

namespace Nexo.Spatial.Platform.ARKit;

/// <summary>
/// Maps ARKit native tracking quality to contract <see cref="PoseSample"/> values.
/// </summary>
internal static class ArKitTrackingStateMapper
{
    internal static PoseSample ToPoseSample(ArKitNativePoseFrame frame) =>
        new(
            new SpatialVector3(frame.PositionX, frame.PositionY, frame.PositionZ),
            new SpatialQuaternion(frame.RotationX, frame.RotationY, frame.RotationZ, frame.RotationW),
            MapConfidence(frame.TrackingQuality),
            frame.Timestamp,
            MapTrackingState(frame.TrackingQuality));

    internal static TrackingState MapTrackingState(ArKitNativeTrackingQuality quality) =>
        quality switch
        {
            ArKitNativeTrackingQuality.Normal => TrackingState.Tracking,
            ArKitNativeTrackingQuality.Limited => TrackingState.Occluded,
            _ => TrackingState.Lost
        };

    internal static double MapConfidence(ArKitNativeTrackingQuality quality) =>
        quality switch
        {
            ArKitNativeTrackingQuality.Normal => 0.95,
            ArKitNativeTrackingQuality.Limited => 0.25,
            _ => 0
        };
}
