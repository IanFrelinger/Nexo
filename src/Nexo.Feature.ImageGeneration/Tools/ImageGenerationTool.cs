using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;
using Nexo.Agent.Models;
using Nexo.Feature.ImageGeneration.Interfaces;
using Nexo.Feature.ImageGeneration.Models;
using Nexo.Feature.ImageGeneration.Services;

namespace Nexo.Feature.ImageGeneration.Tools;

/// <summary>
/// Agent tool for image generation that integrates with the Nexo Agent system
/// </summary>
public class ImageGenerationTool : ITool<ImageGenerationToolRequest, ImageGenerationToolResult>
{
    private readonly ILogger<ImageGenerationTool> _logger;
    private readonly ImageGenerationOrchestrator _orchestrator;

    public ImageGenerationTool(
        ILogger<ImageGenerationTool> logger,
        ImageGenerationOrchestrator orchestrator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
    }

    public ToolManifest Manifest => new(
        Id: "tool.image.generation",
        Name: "Image Generation",
        Version: "1.0.0",
        Description: "Generates images from text prompts using AI models",
        Author: "Nexo Image Generation",
        InputSchema: """
        {
            "type": "object",
            "properties": {
                "prompt": {"type": "string", "description": "Text prompt describing the image to generate"},
                "width": {"type": "integer", "default": 512, "description": "Width of the generated image"},
                "height": {"type": "integer", "default": 512, "description": "Height of the generated image"},
                "count": {"type": "integer", "default": 1, "description": "Number of images to generate"},
                "style": {"type": "string", "description": "Style or model to use"},
                "providerId": {"type": "string", "description": "Specific provider to use (optional)"}
            },
            "required": ["prompt"]
        }
        """,
        OutputSchema: """
        {
            "type": "object",
            "properties": {
                "success": {"type": "boolean"},
                "images": {"type": "array", "items": {"type": "string"}, "description": "Generated images as base64 strings"},
                "imagePaths": {"type": "array", "items": {"type": "string"}, "description": "File paths where images were saved"},
                "error": {"type": "string", "description": "Error message if generation failed"},
                "generationTimeMs": {"type": "integer", "description": "Generation time in milliseconds"}
            }
        }
        """,
        RequiredPermissions: ToolPermissions.FileWrite,
        Capabilities: new[] { "image-generation", "ai", "offline", "online" },
        Timeout: TimeSpan.FromMinutes(5),
        CreatedAt: DateTime.UtcNow
    );

    public async Task<string> ExecuteAsync(string input, ToolContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Parse JSON input
            var request = System.Text.Json.JsonSerializer.Deserialize<ImageGenerationToolRequest>(input);
            if (request == null)
            {
                throw new ArgumentException("Invalid JSON input for image generation tool");
            }

            // Execute with typed input
            var result = await ExecuteAsync(request, context, cancellationToken);
            
            // Serialize result to JSON
            return System.Text.Json.JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing image generation tool with JSON input");
            var errorResult = new ImageGenerationToolResult
            {
                Success = false,
                Error = ex.Message,
                ToolId = Manifest.Id,
                ExecutedAt = DateTime.UtcNow
            };
            return System.Text.Json.JsonSerializer.Serialize(errorResult);
        }
    }

    public async Task<ImageGenerationToolResult> ExecuteAsync(ImageGenerationToolRequest input, ToolContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Executing image generation tool for prompt: {Prompt}", input.Prompt);

            // Create image generation request
            var request = new ImageGenerationRequest
            {
                Prompt = input.Prompt,
                NegativePrompt = input.NegativePrompt,
                Width = input.Width,
                Height = input.Height,
                Count = input.Count,
                Steps = input.Steps,
                GuidanceScale = input.GuidanceScale,
                Seed = input.Seed,
                Style = input.Style,
                Parameters = input.Parameters
            };

            // Generate images
            ImageGenerationResult result;
            if (!string.IsNullOrEmpty(input.ProviderId))
            {
                result = await _orchestrator.GenerateImagesAsync(input.ProviderId, request, cancellationToken);
            }
            else
            {
                result = await _orchestrator.GenerateImagesAsync(request, cancellationToken);
            }

            // Convert result to tool result
            return new ImageGenerationToolResult
            {
                Success = result.Success,
                Images = result.Images,
                ImagePaths = result.ImagePaths,
                Metadata = result.Metadata,
                Error = result.Error,
                GenerationTimeMs = result.GenerationTimeMs,
                ToolId = Manifest.Id,
                ExecutedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing image generation tool");
            return new ImageGenerationToolResult
            {
                Success = false,
                Error = ex.Message,
                ToolId = Manifest.Id,
                ExecutedAt = DateTime.UtcNow
            };
        }
    }
}

/// <summary>
/// Request for the image generation tool
/// </summary>
public record ImageGenerationToolRequest
{
    /// <summary>
    /// Text prompt describing the image to generate
    /// </summary>
    public string Prompt { get; init; } = string.Empty;
    
    /// <summary>
    /// Negative prompt (what to avoid in the image)
    /// </summary>
    public string? NegativePrompt { get; init; }
    
    /// <summary>
    /// Width of the generated image
    /// </summary>
    public int Width { get; init; } = 512;
    
    /// <summary>
    /// Height of the generated image
    /// </summary>
    public int Height { get; init; } = 512;
    
    /// <summary>
    /// Number of images to generate
    /// </summary>
    public int Count { get; init; } = 1;
    
    /// <summary>
    /// Quality/sampling steps (higher = better quality, slower)
    /// </summary>
    public int Steps { get; init; } = 20;
    
    /// <summary>
    /// Guidance scale (how closely to follow the prompt)
    /// </summary>
    public double GuidanceScale { get; init; } = 7.5;
    
    /// <summary>
    /// Random seed for reproducible results
    /// </summary>
    public int? Seed { get; init; }
    
    /// <summary>
    /// Style or model to use
    /// </summary>
    public string? Style { get; init; }
    
    /// <summary>
    /// Specific provider to use (optional)
    /// </summary>
    public string? ProviderId { get; init; }
    
    /// <summary>
    /// Additional parameters specific to the provider
    /// </summary>
    public Dictionary<string, object> Parameters { get; init; } = new();
}

/// <summary>
/// Result from the image generation tool
/// </summary>
public record ImageGenerationToolResult
{
    /// <summary>
    /// Whether the generation was successful
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// Generated images as base64 strings
    /// </summary>
    public List<string> Images { get; init; } = new();
    
    /// <summary>
    /// File paths where images were saved
    /// </summary>
    public List<string> ImagePaths { get; init; } = new();
    
    /// <summary>
    /// Generation metadata
    /// </summary>
    public ImageGenerationMetadata Metadata { get; init; } = new();
    
    /// <summary>
    /// Error message if generation failed
    /// </summary>
    public string? Error { get; init; }
    
    /// <summary>
    /// Generation time in milliseconds
    /// </summary>
    public long GenerationTimeMs { get; init; }
    
    /// <summary>
    /// Tool ID that executed the request
    /// </summary>
    public string ToolId { get; init; } = string.Empty;
    
    /// <summary>
    /// When the tool was executed
    /// </summary>
    public DateTime ExecutedAt { get; init; }
}
