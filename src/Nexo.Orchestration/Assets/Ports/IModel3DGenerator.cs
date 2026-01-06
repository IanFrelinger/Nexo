namespace Nexo.Orchestration.Assets.Ports;

/// <summary>
/// Port for 3D model generation services.
/// 
/// Defines the contract for 3D model generation adapters:
/// - Generate 3D models from text prompts
/// - Generate 3D models from reference images
/// - Support multiple formats and quality levels
/// 
/// Implementations (TripoModelGenerator, MeshyModelGenerator, etc.) provide
/// specific 3D model generation logic. Used by Model3DAssetAgent.
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
/// 
/// Contains:
/// - Text prompt for 3D model generation
/// - Output format (GLB, GLTF, FBX, etc.)
/// - Whether to generate textures
/// - Optional target polygon count
/// - Quality level (draft to production)
/// 
/// Used by IModel3DGenerator to generate 3D models.
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
/// 
/// Defines 3D model file formats:
/// - GLB: Binary glTF (recommended for web)
/// - GLTF: Text-based glTF
/// - FBX: Autodesk FBX format
/// - OBJ: Wavefront OBJ format
/// - USD: Universal Scene Description
/// 
/// Used by IModel3DGenerator to specify output format.
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
/// 
/// Defines quality tiers:
/// - Draft: Fast, low quality
/// - Low: Basic quality
/// - Medium: Balanced quality and speed
/// - High: High quality
/// - Production: Maximum quality
/// 
/// Used by IModel3DGenerator to specify generation quality.
/// </summary>
public enum ModelQuality
{
    Draft,
    Low,
    Medium,
    High,
    Production
}

