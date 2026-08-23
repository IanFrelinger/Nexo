namespace Ashlar.Spatial.Contracts;

/// <summary>
/// Platform-agnostic unit quaternion (X, Y, Z, W).
/// </summary>
/// <param name="X">X component of the quaternion.</param>
/// <param name="Y">Y component of the quaternion.</param>
/// <param name="Z">Z component of the quaternion.</param>
/// <param name="W">W (scalar) component of the quaternion.</param>
public sealed record SpatialQuaternion(double X, double Y, double Z, double W);
