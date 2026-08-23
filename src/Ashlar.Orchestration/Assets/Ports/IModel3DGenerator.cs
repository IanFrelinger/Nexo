namespace Ashlar.Orchestration.Assets.Ports;

/// <summary>
/// Port for 3D model generation services.
/// 
/// Defines the contract for 3D model generation adapters:
/// - Generate 3D models from text prompts
/// - Generate 3D models from reference images
/// - Support multiple formats and quality levels
/// 
/// Implementations (TripoModelGenerator, MeshyModelGenerator, etc.) provide
/// specific 3D model generation logic. Consumed by whichever agent needs models —
/// whichever agent a package supplies for 3D model generation.
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
