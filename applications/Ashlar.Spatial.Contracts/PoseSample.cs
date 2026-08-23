namespace Ashlar.Spatial.Contracts;

/// <summary>
/// Single pose observation for a tracked atom.
/// </summary>
/// <param name="Position">World-space position in meters.</param>
/// <param name="Rotation">World-space orientation as a unit quaternion.</param>
/// <param name="Confidence">Provider confidence in this sample (0.0–1.0).</param>
/// <param name="Timestamp">UTC time when the sample was captured.</param>
/// <param name="TrackingState">Raw tracking quality reported by the provider.</param>
public sealed record PoseSample(
    SpatialVector3 Position,
    SpatialQuaternion Rotation,
    double Confidence,
    DateTimeOffset Timestamp,
    TrackingState TrackingState);
