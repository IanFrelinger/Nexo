namespace Ashlar.Commercial.GameDomain.Assets;
/// <summary>
/// Three-component vector used for positions, rotations, and scales in asset descriptors.
/// </summary>
/// <param name="X">X component.</param>
/// <param name="Y">Y component.</param>
/// <param name="Z">Z component.</param>
public sealed record Vector3Descriptor(
    double X = 0,
    double Y = 0,
    double Z = 0);
