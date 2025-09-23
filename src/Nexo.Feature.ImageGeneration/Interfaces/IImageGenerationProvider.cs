using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.ImageGeneration.Models;

namespace Nexo.Feature.ImageGeneration.Interfaces;

/// <summary>
/// Interface for image generation providers
/// </summary>
public interface IImageGenerationProvider
{
    /// <summary>
    /// Unique identifier for this provider
    /// </summary>
    string ProviderId { get; }
    
    /// <summary>
    /// Human-readable display name
    /// </summary>
    string DisplayName { get; }
    
    /// <summary>
    /// Whether this provider requires online connectivity
    /// </summary>
    bool RequiresOnline { get; }
    
    /// <summary>
    /// Whether this provider is currently available
    /// </summary>
    bool IsAvailable { get; }
    
    /// <summary>
    /// Gets all available models from this provider
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of available image generation models</returns>
    Task<IEnumerable<ImageGenerationModel>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates images based on the provided request
    /// </summary>
    /// <param name="request">Image generation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated images result</returns>
    Task<ImageGenerationResult> GenerateImagesAsync(ImageGenerationRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates images with a specific model
    /// </summary>
    /// <param name="model">Model to use for generation</param>
    /// <param name="request">Image generation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated images result</returns>
    Task<ImageGenerationResult> GenerateImagesAsync(ImageGenerationModel model, ImageGenerationRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the health status of this provider
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health status information</returns>
    Task<ImageGenerationProviderHealth> GetHealthAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Information about an image generation model
/// </summary>
public record ImageGenerationModel
{
    /// <summary>
    /// Unique identifier for the model
    /// </summary>
    public string Id { get; init; } = string.Empty;
    
    /// <summary>
    /// Human-readable name
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Model description
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Whether the model is currently available
    /// </summary>
    public bool IsAvailable { get; init; }
    
    /// <summary>
    /// Supported image dimensions
    /// </summary>
    public List<ImageDimension> SupportedDimensions { get; init; } = new();
    
    /// <summary>
    /// Maximum number of images that can be generated in one request
    /// </summary>
    public int MaxImagesPerRequest { get; init; } = 1;
    
    /// <summary>
    /// Maximum prompt length
    /// </summary>
    public int MaxPromptLength { get; init; } = 1000;
    
    /// <summary>
    /// Model capabilities
    /// </summary>
    public ImageGenerationCapabilities Capabilities { get; init; } = new();
}

/// <summary>
/// Supported image dimensions
/// </summary>
public record ImageDimension
{
    /// <summary>
    /// Width in pixels
    /// </summary>
    public int Width { get; init; }
    
    /// <summary>
    /// Height in pixels
    /// </summary>
    public int Height { get; init; }
    
    /// <summary>
    /// Aspect ratio description
    /// </summary>
    public string AspectRatio { get; init; } = string.Empty;
}

/// <summary>
/// Image generation model capabilities
/// </summary>
public record ImageGenerationCapabilities
{
    /// <summary>
    /// Whether the model supports negative prompts
    /// </summary>
    public bool SupportsNegativePrompt { get; init; }
    
    /// <summary>
    /// Whether the model supports custom seeds
    /// </summary>
    public bool SupportsSeed { get; init; }
    
    /// <summary>
    /// Whether the model supports style parameters
    /// </summary>
    public bool SupportsStyle { get; init; }
    
    /// <summary>
    /// Whether the model supports guidance scale
    /// </summary>
    public bool SupportsGuidanceScale { get; init; }
    
    /// <summary>
    /// Whether the model supports step control
    /// </summary>
    public bool SupportsSteps { get; init; }
    
    /// <summary>
    /// Whether the model supports batch generation
    /// </summary>
    public bool SupportsBatchGeneration { get; init; }
}

/// <summary>
/// Health status of an image generation provider
/// </summary>
public record ImageGenerationProviderHealth
{
    /// <summary>
    /// Whether the provider is healthy
    /// </summary>
    public bool IsHealthy { get; init; }
    
    /// <summary>
    /// Health status message
    /// </summary>
    public string Status { get; init; } = string.Empty;
    
    /// <summary>
    /// Response time in milliseconds
    /// </summary>
    public long ResponseTimeMs { get; init; }
    
    /// <summary>
    /// Available models count
    /// </summary>
    public int AvailableModelsCount { get; init; }
    
    /// <summary>
    /// Last health check timestamp
    /// </summary>
    public DateTime LastChecked { get; init; } = DateTime.UtcNow;
}
