namespace Nexo.Spatial.Contracts;

/// <summary>
/// Platform-agnostic 3D vector (no rendering/math SDK dependency).
/// </summary>
public sealed record SpatialVector3(double X, double Y, double Z);

/// <summary>
/// Platform-agnostic unit quaternion (X, Y, Z, W).
/// </summary>
public sealed record SpatialQuaternion(double X, double Y, double Z, double W);
