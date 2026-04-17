namespace Nexo.GameDomain.Descriptors;

/// <summary>
/// Spatial dimensions for a map element or game object, expressed in world units.
/// </summary>
/// <param name="Width">Extent along the X axis.</param>
/// <param name="Height">Extent along the Y axis.</param>
/// <param name="Depth">Extent along the Z axis.</param>
public sealed record Dimensions(
    double Width = 1.0,
    double Height = 1.0,
    double Depth = 1.0);
