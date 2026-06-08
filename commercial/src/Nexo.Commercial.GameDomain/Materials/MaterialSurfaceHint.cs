namespace Nexo.GameDomain.Materials;

/// <summary>
/// A suggested procedural colour or parameter binding for a logical surface role (host maps to shaders/materials).
/// </summary>
public sealed record MaterialSurfaceHint(
    string Role,
    string ColorOrParameterHint,
    string Note);
