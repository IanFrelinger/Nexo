namespace Nexo.Orchestration.Assets.Ports;

/// <summary>
/// Port for image generation services.
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

/// <summary>
/// Request for image generation.
/// </summary>
public sealed record ImageGenerationRequest
{
    public required string Prompt { get; init; }
    public string? NegativePrompt { get; init; }
    public ImageSize Size { get; init; } = ImageSize.Square1024;
    public string? Style { get; init; }
    public int? Seed { get; init; }
    public double? GuidanceScale { get; init; }
}

/// <summary>
/// Generated image result.
/// </summary>
public sealed record GeneratedImage
{
    public required string FilePath { get; init; }
    public required ImageSize Size { get; init; }
    public required string MimeType { get; init; }
    public int? Seed { get; init; }
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();
}

/// <summary>
/// Standard image sizes for generation.
/// </summary>
public enum ImageSize
{
    Square256,
    Square512,
    Square1024,
    Portrait768x1024,
    Landscape1024x768,
    Wide1920x1080
}

