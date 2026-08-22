namespace Ashlar.Orchestration.Assets.Ports;

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
    /// <summary>Text prompt describing the desired model.</summary>
    public required string Prompt { get; init; }

    /// <summary>Output file format for the generated model.</summary>
    public Model3DFormat OutputFormat { get; init; } = Model3DFormat.GLB;

    /// <summary>Whether to generate textures with the mesh.</summary>
    public bool GenerateTextures { get; init; } = true;

    /// <summary>Target polygon count, if constrained.</summary>
    public int? TargetPolyCount { get; init; }

    /// <summary>Requested generation quality level.</summary>
    public ModelQuality Quality { get; init; } = ModelQuality.Medium;
}
