namespace Nexo.Commercial.GameDomain.Assets;

/// <summary>Cover point record.</summary>
public sealed record CoverPoint
{
    /// <summary>Position value.</summary>
    public Vector3Descriptor Position { get; init; } = new();
    /// <summary>Cover direction value.</summary>
    public Vector3Descriptor CoverDirection { get; init; } = new(); // Direction the cover faces (where threat comes from)
    /// <summary>Cover type value.</summary>
    public string CoverType { get; init; } = "half"; // half, full, lean_left, lean_right
    /// <summary>Rating value.</summary>
    public double Rating { get; init; } = 1.0; // 0-1 quality rating for AI selection
}
