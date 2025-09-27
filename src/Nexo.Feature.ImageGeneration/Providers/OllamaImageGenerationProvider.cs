using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.ImageGeneration.Interfaces;
using Nexo.Feature.ImageGeneration.Models;

namespace Nexo.Feature.ImageGeneration.Providers;

/// <summary>
/// Offline image generation provider using Ollama with local models
/// </summary>
public partial class OllamaImageGenerationProvider : IImageGenerationProvider
{
    private readonly ILogger<OllamaImageGenerationProvider> _logger;
    private readonly List<ImageGenerationModel> _availableModels;
    private bool _isInitialized;

    public OllamaImageGenerationProvider(ILogger<OllamaImageGenerationProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _availableModels = new List<ImageGenerationModel>();
        _isInitialized = false;
    }

    public string ProviderId => "ollama-image-generation";
    public string DisplayName => "Ollama Image Generation (Offline)";
    public bool RequiresOnline => false;
    public bool IsAvailable => _isInitialized && _availableModels.Any(m => m.IsAvailable);

    public async Task<IEnumerable<ImageGenerationModel>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            await InitializeAsync(cancellationToken);
        }

        return _availableModels;
    }

    public async Task<ImageGenerationResult> GenerateImagesAsync(ImageGenerationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating images with Ollama for prompt: {Prompt}", request.Prompt);

            if (!_isInitialized)
            {
                await InitializeAsync(cancellationToken);
            }

            var availableModels = _availableModels.Where(m => m.IsAvailable).ToList();
            if (!availableModels.Any())
            {
                return new ImageGenerationResult
                {
                    Success = false,
                    Error = "No available image generation models found"
                };
            }

            // Use the first available model
            var model = availableModels.First();
            return await GenerateImagesAsync(model, request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating images with Ollama");
            return new ImageGenerationResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<ImageGenerationResult> GenerateImagesAsync(ImageGenerationModel model, ImageGenerationRequest request, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Generating images with model {Model} for prompt: {Prompt}", model.Name, request.Prompt);

            // Create the generation prompt
            var prompt = CreateGenerationPrompt(request);
            
            // Simulate image generation (in a real implementation, this would call Ollama API)
            _logger.LogInformation("Simulating image generation with Ollama model: {Model}", model.Id);
            
            // Create a placeholder image
            var placeholderImage = await CreatePlaceholderImageAsync(request.Width, request.Height, request.Prompt);
            
            var images = new List<string>();
            var imagePaths = new List<string>();

            // Convert to base64 and save
            var base64Image = Convert.ToBase64String(placeholderImage);
            images.Add(base64Image);

            var imagePath = await SaveImageToFileAsync(placeholderImage, request.Prompt);
            if (!string.IsNullOrEmpty(imagePath))
            {
                imagePaths.Add(imagePath);
            }

            var generationTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            return new ImageGenerationResult
            {
                Success = true,
                Images = images,
                ImagePaths = imagePaths,
                Metadata = new ImageGenerationMetadata
                {
                    Model = model.Name,
                    Provider = ProviderId,
                    Parameters = request,
                    GeneratedAt = startTime
                },
                GenerationTimeMs = (long)generationTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating images with model {Model}", model.Name);
            return new ImageGenerationResult
            {
                Success = false,
                Error = ex.Message,
                GenerationTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }
    }

    public async Task<ImageGenerationProviderHealth> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            if (!_isInitialized)
            {
                await InitializeAsync(cancellationToken);
            }

            var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            var availableModelsCount = _availableModels.Count(m => m.IsAvailable);

            return new ImageGenerationProviderHealth
            {
                IsHealthy = availableModelsCount > 0,
                Status = availableModelsCount > 0 ? "Healthy" : "No models available",
                ResponseTimeMs = (long)responseTime,
                AvailableModelsCount = availableModelsCount,
                LastChecked = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for Ollama image generation provider");
            return new ImageGenerationProviderHealth
            {
                IsHealthy = false,
                Status = $"Error: {ex.Message}",
                ResponseTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds,
                AvailableModelsCount = 0,
                LastChecked = DateTime.UtcNow
            };
        }
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Initializing Ollama image generation provider");

            // Simulate checking for available models (in a real implementation, this would call Ollama API)
            _logger.LogInformation("Simulating Ollama model discovery");
            
            // Create mock image generation models
            var imageModels = new[]
            {
                "stable-diffusion-xl",
                "dall-e-2",
                "image-generation-v1"
            };

            _availableModels.Clear();
            foreach (var model in imageModels)
            {
                _availableModels.Add(new ImageGenerationModel
                {
                    Id = model,
                    Name = model,
                    Description = $"Ollama image generation model: {model}",
                    IsAvailable = true,
                    SupportedDimensions = new List<ImageDimension>
                    {
                        new() { Width = 512, Height = 512, AspectRatio = "1:1" },
                        new() { Width = 768, Height = 768, AspectRatio = "1:1" },
                        new() { Width = 1024, Height = 1024, AspectRatio = "1:1" },
                        new() { Width = 512, Height = 768, AspectRatio = "2:3" },
                        new() { Width = 768, Height = 512, AspectRatio = "3:2" }
                    },
                    MaxImagesPerRequest = 4,
                    MaxPromptLength = 2000,
                    Capabilities = new ImageGenerationCapabilities
                    {
                        SupportsNegativePrompt = true,
                        SupportsSeed = true,
                        SupportsStyle = true,
                        SupportsGuidanceScale = true,
                        SupportsSteps = true,
                        SupportsBatchGeneration = true
                    }
                });
            }

            _isInitialized = true;
            _logger.LogInformation("Ollama image generation provider initialized with {ModelCount} models", _availableModels.Count);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Ollama image generation provider");
            _isInitialized = false;
        }
    }

    private string CreateGenerationPrompt(ImageGenerationRequest request)
    {
        var prompt = request.Prompt;

        if (!string.IsNullOrEmpty(request.NegativePrompt))
        {
            prompt += $", negative: {request.NegativePrompt}";
        }

        if (!string.IsNullOrEmpty(request.Style))
        {
            prompt += $", style: {request.Style}";
        }

        return prompt;
    }

    private async Task<byte[]> CreatePlaceholderImageAsync(int width, int height, string prompt)
    {
        // Create a simple placeholder image using SkiaSharp (cross-platform)
        var info = new SkiaSharp.SKImageInfo(width, height, SkiaSharp.SKColorType.Rgba8888, SkiaSharp.SKAlphaType.Premul);
        using var surface = SkiaSharp.SKSurface.Create(info);
        using var canvas = surface.Canvas;
        
        // Fill with a gradient background
        using var shader = SkiaSharp.SKShader.CreateLinearGradient(
            new SkiaSharp.SKPoint(0, 0),
            new SkiaSharp.SKPoint(0, height),
            new SkiaSharp.SKColor[] { 
                new SkiaSharp.SKColor(25, 25, 112), // DarkBlue
                new SkiaSharp.SKColor(128, 0, 128)  // Purple
            },
            new float[] { 0, 1 },
            SkiaSharp.SKShaderTileMode.Clamp);
        
        using var paint = new SkiaSharp.SKPaint { Shader = shader };
        canvas.DrawRect(0, 0, width, height, paint);
        
        // Add text overlay
        using var textPaint = new SkiaSharp.SKPaint
        {
            Color = SkiaSharp.SKColors.White,
            TextSize = 16,
            IsAntialias = true,
            Typeface = SkiaSharp.SKTypeface.Default
        };
        
        var text = $"Generated: {prompt[..Math.Min(prompt.Length, 30)]}...";
        var textBounds = new SkiaSharp.SKRect();
        textPaint.MeasureText(text, ref textBounds);
        
        var x = (width - textBounds.Width) / 2;
        var y = (height + textBounds.Height) / 2;
        
        canvas.DrawText(text, x, y, textPaint);
        
        // Convert to byte array
        using var image = surface.Snapshot();
        using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
        
        await Task.CompletedTask;
        return data.ToArray();
    }

    private async Task<string?> SaveImageToFileAsync(byte[] imageData, string prompt)
    {
        try
        {
            var fileName = $"ollama_generated_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.png";
            var outputDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Nexo", "GeneratedImages");
            
            Directory.CreateDirectory(outputDir);
            var filePath = Path.Combine(outputDir, fileName);

            await File.WriteAllBytesAsync(filePath, imageData);
            _logger.LogInformation("Image saved to: {FilePath}", filePath);

            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save generated image to file");
            return null;
        }
    }
}
