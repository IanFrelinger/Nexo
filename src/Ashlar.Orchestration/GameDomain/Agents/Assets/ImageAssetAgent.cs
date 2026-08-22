using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;
using Ashlar.Orchestration.Architect.Models;
using Ashlar.Orchestration.Assets.Models;
using Ashlar.Orchestration.Assets.Ports;
using System.Text.RegularExpressions;

using Ashlar.Orchestration.Agents;
using Ashlar.Orchestration.Agents.Assets;

namespace Ashlar.Orchestration.GameDomain.Agents.Assets;

/// <summary>
/// Agent that generates image assets for games.
/// 
/// Specialized agent for image generation that:
/// - Uses IImageGenerator to create game assets
/// - Handles prompt generation and refinement
/// - Manages asset storage and retrieval
/// - Supports various image sizes and styles
/// - Implements retry logic and validation
/// 
/// Inherits from BaseAssetAgent for common asset generation functionality.
/// </summary>
public sealed class ImageAssetAgent : BaseAssetAgent
{
    private readonly IImageGenerator _imageGenerator;

    public override AssetType AssetType => AssetType.Image;

    public ImageAssetAgent(
        AgentSpawnSpec spec,
        IImageGenerator imageGenerator,
        IModel? model,
        IAssetStorage assetStorage,
        ILogger<BaseAgent> logger) : base(spec, model, assetStorage, logger)
    {
        _imageGenerator = imageGenerator ?? throw new ArgumentNullException(nameof(imageGenerator));
    }

    protected override async Task<GeneratedAssetBase> GenerateAssetAsync(
        GenerationPrompt prompt,
        CancellationToken cancellationToken)
    {
        var request = new ImageGenerationRequest
        {
            Prompt = prompt.TextPrompt,
            NegativePrompt = prompt.NegativePrompt,
            Size = DetermineSize(prompt.Parameters),
            Style = prompt.StyleReference
        };

        var result = await _imageGenerator.GenerateAsync(request, cancellationToken);

        return new GeneratedImageAsset
        {
            FilePath = result.FilePath,
            Size = result.Size,
            MimeType = result.MimeType
        };
    }

    protected override string GetPromptGenerationSystemPrompt()
    {
        return """
            You are an expert at crafting prompts for AI image generation.
            Given a game asset requirement, generate a detailed prompt that will produce
            a high-quality game asset.

            Consider:
            - Art style (realistic, stylized, pixel art, etc.)
            - Lighting and mood
            - Camera angle and framing
            - Technical requirements (transparency, tileable, etc.)
            - Game context and consistency

            Respond with JSON:
            {
                "prompt": "detailed generation prompt",
                "negativePrompt": "things to avoid (optional)",
                "style": "art style reference (optional)"
            }
            """;
    }

    protected override ConstraintResult EvaluateConstraint(
        GeneratedAssetBase asset,
        AgentConstraint constraint)
    {
        var imageAsset = (GeneratedImageAsset)asset;
        var desc = constraint.Description.ToLowerInvariant();

        // Check resolution constraints
        if (desc.Contains("resolution") || desc.Contains("size"))
        {
            var match = Regex.Match(desc, @"(\d+)\s*x\s*(\d+)");
            if (match.Success)
            {
                var reqWidth = int.Parse(match.Groups[1].Value);
                var reqHeight = int.Parse(match.Groups[2].Value);
                var (actWidth, actHeight) = GetImageDimensions(imageAsset.Size);

                if (actWidth != reqWidth || actHeight != reqHeight)
                {
                    return constraint.IsMandatory ? ConstraintResult.Failed : ConstraintResult.Warning;
                }
            }
        }

        // Check format constraints
        if (desc.Contains("png") && !imageAsset.MimeType.Contains("png"))
        {
            return constraint.IsMandatory ? ConstraintResult.Failed : ConstraintResult.Warning;
        }

        return ConstraintResult.Passed;
    }

    protected override string GetMimeType(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    private ImageSize DetermineSize(IReadOnlyDictionary<string, object> parameters)
    {
        if (parameters.TryGetValue("width", out var w) && parameters.TryGetValue("height", out var h))
        {
            var width = Convert.ToInt32(w);
            var height = Convert.ToInt32(h);

            if (width == height)
            {
                return width <= 512 ? ImageSize.Square512 : ImageSize.Square1024;
            }
            if (width > height)
            {
                return ImageSize.Landscape1024x768;
            }
            return ImageSize.Portrait768x1024;
        }

        return ImageSize.Square1024;
    }

    private (int Width, int Height) GetImageDimensions(ImageSize size)
    {
        return size switch
        {
            ImageSize.Square256 => (256, 256),
            ImageSize.Square512 => (512, 512),
            ImageSize.Square1024 => (1024, 1024),
            ImageSize.Portrait768x1024 => (768, 1024),
            ImageSize.Landscape1024x768 => (1024, 768),
            ImageSize.Wide1920x1080 => (1920, 1080),
            _ => (1024, 1024)
        };
    }
}
