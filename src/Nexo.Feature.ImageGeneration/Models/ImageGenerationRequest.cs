using System.Collections.Generic;

namespace Nexo.Feature.ImageGeneration.Models;

/// <summary>
/// Request for image generation
/// </summary>
public record ImageGenerationRequest
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
    /// Additional parameters specific to the provider
    /// </summary>
    public Dictionary<string, object> Parameters { get; init; } = new();
}

/// <summary>
/// Result of image generation
/// </summary>
public record ImageGenerationResult
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
    /// File paths where images were saved (if saved to disk)
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
}

/// <summary>
/// Metadata about the image generation process
/// </summary>
public record ImageGenerationMetadata
{
    /// <summary>
    /// Model used for generation
    /// </summary>
    public string Model { get; init; } = string.Empty;
    
    /// <summary>
    /// Provider used (OpenAI, Stability AI, Ollama, etc.)
    /// </summary>
    public string Provider { get; init; } = string.Empty;
    
    /// <summary>
    /// Parameters used for generation
    /// </summary>
    public ImageGenerationRequest Parameters { get; init; } = new();
    
    /// <summary>
    /// Timestamp when generation started
    /// </summary>
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Cost in credits/tokens (if applicable)
    /// </summary>
    public decimal? Cost { get; init; }
}
