namespace Ashlar.Orchestration.Assets.Ports;

/// <summary>
/// Port for image generation services.
/// 
/// Defines the contract for image generation adapters:
/// - Generate images from text prompts
/// - Generate image variations
/// - Support multiple sizes and styles
/// 
/// Implementations (DalleImageGenerator, LocalImageGenerator, etc.) provide
/// specific image generation logic. Used by ImageAssetAgent.
/// </summary>
public interface IImageGenerator
{
    /// <summary>
    /// Generates an image from a text prompt.
    /// </summary>
    Task<GeneratedImage> GenerateAsync(
        ImageGenerationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates variations of an existing image.
    /// </summary>
    Task<IReadOnlyList<GeneratedImage>> GenerateVariationsAsync(
        string sourceImagePath,
        int count,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Supported image sizes.
    /// </summary>
    IReadOnlyList<ImageSize> SupportedSizes { get; }

    /// <summary>
    /// Supported styles/models.
    /// </summary>
    IReadOnlyList<string> SupportedStyles { get; }
}
