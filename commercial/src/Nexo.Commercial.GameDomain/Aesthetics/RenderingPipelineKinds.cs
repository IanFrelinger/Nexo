namespace Nexo.GameDomain.Aesthetics;

/// <summary>
/// Optional semantic pipeline hint for <see cref="AestheticPack.RenderingPipelineKind"/>.
/// Hosts map these to engine-specific render paths (URP/HDRP, Forward+, Godot Forward+, etc.).
/// </summary>
public static class RenderingPipelineKinds
{
    public const string ForwardStylized = "forward_stylized";
    public const string ForwardPbr = "forward_pbr";
    public const string DeferredPbr = "deferred_pbr";
    public const string UnlitFlat = "unlit_flat";
    public const string Auto = "auto";
}
