namespace Nexo.Commercial.GameDomain.Aesthetics;
/// <summary>
/// Binds a logical rendering role to an engine-specific material or shader surface.
/// </summary>
public sealed record EngineSurfaceBinding
{
    /// <summary>Engine identifier, e.g. <c>unity</c>, <c>unreal</c>, <c>godot</c>.</summary>
    public string EngineId { get; init; } = string.Empty;

    /// <summary>Logical role, e.g. <c>world_primary</c>, <c>character_skin</c>.</summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>Engine-neutral material intent id.</summary>
    public string MaterialSurfaceId { get; init; } = string.Empty;

    /// <summary>Optional shader or asset path hint for the target engine.</summary>
    public string? AssetOrShaderHint { get; init; }

    /// <summary>Optional key/value parameters for the host importer.</summary>
    public IReadOnlyDictionary<string, string> Parameters { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
