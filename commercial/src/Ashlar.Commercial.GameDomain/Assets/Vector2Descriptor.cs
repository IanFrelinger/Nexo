namespace Ashlar.Commercial.GameDomain.Assets;
/// <summary>
/// Two-component vector used for UI element sizes and positions.
/// </summary>
/// <param name="X">X component.</param>
/// <param name="Y">Y component.</param>
public sealed record Vector2Descriptor(
    double X = 0,
    double Y = 0);
