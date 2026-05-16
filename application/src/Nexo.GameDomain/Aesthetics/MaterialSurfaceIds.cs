namespace Nexo.GameDomain.Aesthetics;

/// <summary>
/// Recommended <see cref="EngineRenderingSurfaceBinding.MaterialSurfaceId"/> values (engine-neutral intent).
/// </summary>
public static class MaterialSurfaceIds
{
    public const string StylizedLit = "stylized_lit";
    public const string ForwardPbr = "forward_pbr";
    public const string UnlitVertexColor = "unlit_vertex_color";
    public const string Unlit = "unlit";

    public static IReadOnlySet<string> Documented { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        StylizedLit, ForwardPbr, UnlitVertexColor, Unlit
    };
}
