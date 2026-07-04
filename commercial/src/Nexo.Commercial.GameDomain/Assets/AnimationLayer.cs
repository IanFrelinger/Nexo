namespace Nexo.Commercial.GameDomain.Assets;

/// <summary>Animation layer record.</summary>
public sealed record AnimationLayer
{
    /// <summary>Name value.</summary>
    public string Name { get; init; } = "Base Layer";
    /// <summary>Weight value.</summary>
    public double Weight { get; init; } = 1.0;
    /// <summary>Blending mode value.</summary>
    public string BlendingMode { get; init; } = "override";
    /// <summary>Avatar mask value.</summary>
    public string? AvatarMask { get; init; }
    /// <summary>Ik pass value.</summary>
    public bool IKPass { get; init; }
}
