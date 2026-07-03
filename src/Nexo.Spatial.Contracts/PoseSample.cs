namespace Nexo.Spatial.Contracts;

/// <summary>
/// Single pose observation for a tracked atom.
/// </summary>
public sealed record PoseSample(
    SpatialVector3 Position,
    SpatialQuaternion Rotation,
    double Confidence,
    DateTimeOffset Timestamp,
    TrackingState TrackingState);
