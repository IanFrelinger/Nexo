namespace Nexo.Spatial.Platform.ARKit.Interop;

/// <summary>
/// Raw pose frame from the native ARKit interop layer (no downstream filtering).
/// </summary>
public sealed record ArKitNativePoseFrame(
    double PositionX,
    double PositionY,
    double PositionZ,
    double RotationX,
    double RotationY,
    double RotationZ,
    double RotationW,
    ArKitNativeTrackingQuality TrackingQuality,
    DateTimeOffset Timestamp);
