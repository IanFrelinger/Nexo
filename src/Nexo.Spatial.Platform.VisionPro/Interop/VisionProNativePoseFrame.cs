namespace Nexo.Spatial.Platform.VisionPro.Interop;

/// <summary>
/// Raw pose frame from the visionOS WorldTracking interop layer.
/// </summary>
public sealed record VisionProNativePoseFrame(
    double PositionX,
    double PositionY,
    double PositionZ,
    double RotationX,
    double RotationY,
    double RotationZ,
    double RotationW,
    VisionProNativeTrackingQuality TrackingQuality,
    DateTimeOffset Timestamp);
