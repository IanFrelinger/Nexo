namespace Ashlar.Commercial.GameDomain.Aesthetics;
/// <summary>
/// Binds a logical rendering role in an <see cref="AestheticPack"/> to an engine-specific material or shader surface.
/// Multiple bindings for the same <see cref="Role"/> and different <see cref="EngineId"/> allow one pack to adapt
/// across Unity, Unreal, Godot, or custom runtimes without changing Ashlar core types.
/// </summary>
public sealed record EngineRenderingSurfaceBinding
{
    /// <summary>Target engine; use <see cref="GameEngines"/> constants when possible.</summary>
    public required string EngineId { get; init; }

    /// <summary>Logical surface role, e.g. <c>world_primary</c>, <c>character_skin</c>, <c>ui_default</c>.</summary>
    public required string Role { get; init; }

    /// <summary>Engine-neutral material intent, e.g. <c>stylized_lit</c>, <c>unlit_vertex_color</c>.</summary>
    public required string MaterialSurfaceId { get; init; }

    /// <summary>Optional engine-specific shader or material asset path / registry id.</summary>
    public string? AssetOrShaderHint { get; init; }

    /// <summary>Optional key/value hints (render queue, feature flags, texture slot ids).</summary>
    public IReadOnlyDictionary<string, string>? Parameters { get; init; }
}
