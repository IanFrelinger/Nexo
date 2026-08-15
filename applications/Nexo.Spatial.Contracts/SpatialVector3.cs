namespace Nexo.Spatial.Contracts;

/// <summary>
/// Platform-agnostic 3D vector (no rendering/math SDK dependency).
/// </summary>
/// <param name="X">X component in meters.</param>
/// <param name="Y">Y component in meters.</param>
/// <param name="Z">Z component in meters.</param>
public sealed record SpatialVector3(double X, double Y, double Z);
