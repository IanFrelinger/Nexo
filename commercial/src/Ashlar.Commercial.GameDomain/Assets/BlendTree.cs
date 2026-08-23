namespace Ashlar.Commercial.GameDomain.Assets;

/// <summary>Blend tree record.</summary>
public sealed record BlendTree
{
    /// <summary>Name value.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Blend parameter value.</summary>
    public string BlendParameter { get; init; } = string.Empty;
    /// <summary>Blend parameter y value.</summary>
    public string? BlendParameterY { get; init; }
    /// <summary>Blend type value.</summary>
    public string BlendType { get; init; } = "1D";
    /// <summary>Children value.</summary>
    public IReadOnlyList<BlendTreeChild> Children { get; init; } = Array.Empty<BlendTreeChild>();
}
