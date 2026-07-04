namespace Nexo.Orchestration.Assets.Ports;

/// <summary>
/// Generated image result.
/// 
/// Contains:
/// - File path where the image is stored
/// - Image size and MIME type
/// - Optional seed used for generation
/// - Optional metadata dictionary
/// 
/// Returned by IImageGenerator after successful generation.
/// </summary>
public sealed record GeneratedImage
{
    /// <summary>Path to the generated image file.</summary>
    public required string FilePath { get; init; }

    /// <summary>Dimensions of the generated image.</summary>
    public required ImageSize Size { get; init; }

    /// <summary>MIME type of the image file.</summary>
    public required string MimeType { get; init; }

    /// <summary>Random seed used for generation, if deterministic.</summary>
    public int? Seed { get; init; }

    /// <summary>Additional generation metadata.</summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();
}
