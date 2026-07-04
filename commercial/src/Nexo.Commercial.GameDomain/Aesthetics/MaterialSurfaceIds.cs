namespace Nexo.Commercial.GameDomain.Aesthetics;
/// <summary>
/// Recommended <see cref="EngineRenderingSurfaceBinding.MaterialSurfaceId"/> values (engine-neutral intent).
/// </summary>
public static class MaterialSurfaceIds
{
    /// <summary>Constant value for stylized lit.</summary>
    public const string StylizedLit = "stylized_lit";
    /// <summary>Constant value for forward pbr.</summary>
    public const string ForwardPbr = "forward_pbr";
    /// <summary>Constant value for unlit vertex color.</summary>
    public const string UnlitVertexColor = "unlit_vertex_color";
    /// <summary>Constant value for unlit.</summary>
    public const string Unlit = "unlit";

    /// <summary>Documented value.</summary>
    public static IReadOnlySet<string> Documented { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        StylizedLit, ForwardPbr, UnlitVertexColor, Unlit
    };
}
