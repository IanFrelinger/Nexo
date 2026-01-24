namespace Nexo.GeoWorld.Materials;

/// <summary>
/// Engine-friendly material assignment metadata.
/// Unity importer can map MaterialName -> Unity material template.
/// </summary>
public sealed record MaterialAssignment
{
    public required string MeshKind { get; init; } // buildings|roads|water|terrain
    public required string MaterialName { get; init; }
    public float? UvMetersPerRepeat { get; init; }
    public string? BaseColorTexturePath { get; init; }
    public IReadOnlyDictionary<string, string>? Tags { get; init; }
}

