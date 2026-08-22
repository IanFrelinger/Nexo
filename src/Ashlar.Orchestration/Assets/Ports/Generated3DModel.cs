namespace Ashlar.Orchestration.Assets.Ports;

/// <summary>
/// Generated 3D model result.
/// 
/// Contains:
/// - File path where the model is stored
/// - Model format and geometry stats (vertices, triangles)
/// - List of texture file paths
/// - Optional metadata dictionary
/// 
/// Returned by IModel3DGenerator after successful generation.
/// </summary>
public sealed record Generated3DModel
{
    /// <summary>Path to the generated model file.</summary>
    public required string FilePath { get; init; }

    /// <summary>Format of the generated model.</summary>
    public required Model3DFormat Format { get; init; }

    /// <summary>Number of vertices in the mesh.</summary>
    public int VertexCount { get; init; }

    /// <summary>Number of triangles in the mesh.</summary>
    public int TriangleCount { get; init; }

    /// <summary>Paths to generated texture files.</summary>
    public IReadOnlyList<string> TexturePaths { get; init; } = Array.Empty<string>();

    /// <summary>Additional generation metadata.</summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();
}
