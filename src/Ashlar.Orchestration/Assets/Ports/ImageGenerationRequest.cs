namespace Ashlar.Orchestration.Assets.Ports;

/// <summary>
/// Request for image generation.
/// 
/// Contains:
/// - Text prompt for image generation
/// - Optional negative prompt
/// - Image size and style
/// - Optional seed and guidance scale
/// 
/// Used by IImageGenerator to generate images.
/// </summary>
public sealed record ImageGenerationRequest
{
    /// <summary>Text prompt describing the desired image.</summary>
    public required string Prompt { get; init; }

    /// <summary>Negative prompt excluding unwanted features.</summary>
    public string? NegativePrompt { get; init; }

    /// <summary>Output image dimensions.</summary>
    public ImageSize Size { get; init; } = ImageSize.Square1024;

    /// <summary>Optional visual style hint.</summary>
    public string? Style { get; init; }

    /// <summary>Random seed for reproducible generation.</summary>
    public int? Seed { get; init; }

    /// <summary>Classifier-free guidance scale.</summary>
    public double? GuidanceScale { get; init; }
}
