namespace Nexo.GeoTerrain;

/// <summary>
/// A simple triangle mesh.
/// </summary>
public sealed record MeshData
{
    public required IReadOnlyList<Vector3> Vertices { get; init; }
    public required IReadOnlyList<int> Indices { get; init; }
    public IReadOnlyList<Vector3>? Normals { get; init; }
    public IReadOnlyList<Vector2>? TexCoords { get; init; }
}

