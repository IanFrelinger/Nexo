namespace Nexo.Orchestration.Assets.Ports;

/// <summary>
/// Port for 3D model generation services.
/// </summary>
public interface IModel3DGenerator
{
    /// <summary>
    /// Generates a 3D model from a text prompt.
    /// </summary>
    Task<Generated3DModel> GenerateFromTextAsync(
        Model3DGenerationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a 3D model from a reference image.
    /// </summary>
    Task<Generated3DModel> GenerateFromImageAsync(
        string imagePath,
        Model3DGenerationRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Request for 3D model generation.
/// </summary>
public sealed record Model3DGenerationRequest
{
    public required string Prompt { get; init; }
    public Model3DFormat OutputFormat { get; init; } = Model3DFormat.GLB;
    public bool GenerateTextures { get; init; } = true;
    public int? TargetPolyCount { get; init; }
    public ModelQuality Quality { get; init; } = ModelQuality.Medium;
}

/// <summary>
/// Generated 3D model result.
/// </summary>
public sealed record Generated3DModel
{
    public required string FilePath { get; init; }
    public required Model3DFormat Format { get; init; }
    public int VertexCount { get; init; }
    public int TriangleCount { get; init; }
    public IReadOnlyList<string> TexturePaths { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();
}

/// <summary>
/// Supported 3D model formats.
/// </summary>
public enum Model3DFormat
{
    GLB,
    GLTF,
    FBX,
    OBJ,
    USD
}

/// <summary>
/// Quality levels for 3D model generation.
/// </summary>
public enum ModelQuality
{
    Draft,
    Low,
    Medium,
    High,
    Production
}

