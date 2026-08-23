namespace Ashlar.Spatial.Platform.XREAL.Interop;

/// <summary>
/// Raw 6DoF pose frame from the NRSDK interop layer (no downstream filtering).
/// </summary>
public sealed record XrealNativePoseFrame(
    double PositionX,
    double PositionY,
    double PositionZ,
    double RotationX,
    double RotationY,
    double RotationZ,
    double RotationW,
    XrealNativeTrackingQuality TrackingQuality,
    DateTimeOffset Timestamp);
