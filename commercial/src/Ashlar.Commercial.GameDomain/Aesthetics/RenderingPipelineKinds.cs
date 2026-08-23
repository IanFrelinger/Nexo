namespace Ashlar.Commercial.GameDomain.Aesthetics;
/// <summary>
/// Optional semantic pipeline hint for <see cref="AestheticPack.RenderingPipelineKind"/>.
/// Hosts map these to engine-specific render paths (URP/HDRP, Forward+, Godot Forward+, etc.).
/// </summary>
public static class RenderingPipelineKinds
{
    /// <summary>Constant value for forward stylized.</summary>
    public const string ForwardStylized = "forward_stylized";
    /// <summary>Constant value for forward pbr.</summary>
    public const string ForwardPbr = "forward_pbr";
    /// <summary>Constant value for deferred pbr.</summary>
    public const string DeferredPbr = "deferred_pbr";
    /// <summary>Constant value for unlit flat.</summary>
    public const string UnlitFlat = "unlit_flat";
    /// <summary>Constant value for auto.</summary>
    public const string Auto = "auto";
}
