using Nexo.Orchestration.Assets.Ports;

namespace Nexo.Adapters.Assets.Images;

/// <summary>
/// Echo/placeholder image generator for testing (doesn't actually generate images).
/// 
/// Provides:
/// - Placeholder image generation for testing
/// - Mock implementation of IImageGenerator
/// - Support for image variations
/// 
/// Used for testing and development when real image generation is not needed.
/// Implements IImageGenerator for use with ImageAssetAgent.
/// </summary>
public sealed class EchoImageGenerator : IImageGenerator
{
    public IReadOnlyList<ImageSize> SupportedSizes => new[]
    {
        ImageSize.Square256,
        ImageSize.Square512,
        ImageSize.Square1024,
        ImageSize.Portrait768x1024,
        ImageSize.Landscape1024x768
    };

    public IReadOnlyList<string> SupportedStyles => new[] { "default" };

    public Task<GeneratedImage> GenerateAsync(
        ImageGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        // Create a placeholder file
        var tempPath = Path.Combine(Path.GetTempPath(), $"echo_image_{Guid.NewGuid()}.png");
        File.WriteAllText(tempPath, "Placeholder image - replace with real generator");

        return Task.FromResult(new GeneratedImage
        {
            FilePath = tempPath,
            Size = request.Size,
            MimeType = "image/png",
            Seed = request.Seed,
            Metadata = new Dictionary<string, object>
            {
                ["generator"] = "EchoImageGenerator",
                ["prompt"] = request.Prompt
            }
        });
    }

    public Task<IReadOnlyList<GeneratedImage>> GenerateVariationsAsync(
        string sourceImagePath,
        int count,
        CancellationToken cancellationToken = default)
    {
        var variations = new List<GeneratedImage>();
        for (int i = 0; i < count; i++)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"echo_variation_{Guid.NewGuid()}.png");
            File.WriteAllText(tempPath, $"Variation {i + 1} of {sourceImagePath}");

            variations.Add(new GeneratedImage
            {
                FilePath = tempPath,
                Size = ImageSize.Square1024,
                MimeType = "image/png",
                Metadata = new Dictionary<string, object>
                {
                    ["variation_index"] = i + 1,
                    ["source"] = sourceImagePath
                }
            });
        }

        return Task.FromResult<IReadOnlyList<GeneratedImage>>(variations);
    }
}

