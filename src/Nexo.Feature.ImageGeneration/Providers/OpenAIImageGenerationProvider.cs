using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.ImageGeneration.Interfaces;
using Nexo.Feature.ImageGeneration.Models;

namespace Nexo.Feature.ImageGeneration.Providers;

/// <summary>
/// Online image generation provider using OpenAI DALL-E API
/// </summary>
public partial class OpenAIImageGenerationProvider : IImageGenerationProvider
{
    private readonly ILogger<OpenAIImageGenerationProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly List<ImageGenerationModel> _availableModels;

    public OpenAIImageGenerationProvider(
        ILogger<OpenAIImageGenerationProvider> logger,
        HttpClient httpClient,
        string apiKey,
        string? baseUrl = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _baseUrl = baseUrl ?? "https://api.openai.com/v1";
        _availableModels = new List<ImageGenerationModel>();
        
        InitializeModels();
    }

    public string ProviderId => "openai-image-generation";
    public string DisplayName => "OpenAI DALL-E (Online)";
    public bool RequiresOnline => true;
    public bool IsAvailable => !string.IsNullOrEmpty(_apiKey) && _availableModels.Any(m => m.IsAvailable);

    public async Task<IEnumerable<ImageGenerationModel>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return _availableModels;
    }

    public async Task<ImageGenerationResult> GenerateImagesAsync(ImageGenerationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating images with OpenAI DALL-E for prompt: {Prompt}", request.Prompt);

            var availableModels = _availableModels.Where(m => m.IsAvailable).ToList();
            if (!availableModels.Any())
            {
                return new ImageGenerationResult
                {
                    Success = false,
                    Error = "No available OpenAI image generation models found"
                };
            }

            // Use DALL-E 3 for best quality, fallback to DALL-E 2
            var model = availableModels.FirstOrDefault(m => m.Id.Contains("dall-e-3")) ?? availableModels.First();
            return await GenerateImagesAsync(model, request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating images with OpenAI DALL-E");
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

            // Prepare the API request
            var apiRequest = new
            {
                model = model.Id,
                prompt = request.Prompt,
                n = Math.Min(request.Count, model.MaxImagesPerRequest),
                size = GetSizeString(request.Width, request.Height),
                quality = request.Steps > 20 ? "hd" : "standard",
                style = request.Style ?? "vivid"
            };

            var json = JsonSerializer.Serialize(apiRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Set up HTTP request
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/images/generations")
            {
                Content = content
            };
            httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

            // Make the API call
            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = JsonSerializer.Deserialize<OpenAIErrorResponse>(responseContent);
                return new ImageGenerationResult
                {
                    Success = false,
                    Error = errorResponse?.Error?.Message ?? $"HTTP {response.StatusCode}: {responseContent}"
                };
            }

            var apiResponse = JsonSerializer.Deserialize<OpenAIImageGenerationResponse>(responseContent);
            if (apiResponse?.Data == null)
            {
                return new ImageGenerationResult
                {
                    Success = false,
                    Error = "Invalid response from OpenAI API"
                };
            }

            var images = new List<string>();
            var imagePaths = new List<string>();

            // Process generated images
            foreach (var imageData in apiResponse.Data)
            {
                if (!string.IsNullOrEmpty(imageData.Url))
                {
                    // Download the image
                    var imageBytes = await _httpClient.GetByteArrayAsync(imageData.Url, cancellationToken);
                    var base64Image = Convert.ToBase64String(imageBytes);
                    images.Add(base64Image);

                    // Save to file
                    var imagePath = await SaveImageToFileAsync(imageBytes, request.Prompt);
                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        imagePaths.Add(imagePath);
                    }
                }
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
                    GeneratedAt = startTime,
                    Cost = CalculateCost(model.Id, request.Count)
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
            // Test API connectivity with a simple request
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/models");
            httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            return new ImageGenerationProviderHealth
            {
                IsHealthy = response.IsSuccessStatusCode,
                Status = response.IsSuccessStatusCode ? "Healthy" : $"HTTP {response.StatusCode}",
                ResponseTimeMs = (long)responseTime,
                AvailableModelsCount = _availableModels.Count(m => m.IsAvailable),
                LastChecked = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for OpenAI image generation provider");
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

    private void InitializeModels()
    {
        _availableModels.AddRange(new[]
        {
            new ImageGenerationModel
            {
                Id = "dall-e-3",
                Name = "DALL-E 3",
                Description = "OpenAI's most advanced image generation model",
                IsAvailable = true,
                SupportedDimensions = new List<ImageDimension>
                {
                    new() { Width = 1024, Height = 1024, AspectRatio = "1:1" },
                    new() { Width = 1024, Height = 1792, AspectRatio = "9:16" },
                    new() { Width = 1792, Height = 1024, AspectRatio = "16:9" }
                },
                MaxImagesPerRequest = 1,
                MaxPromptLength = 4000,
                Capabilities = new ImageGenerationCapabilities
                {
                    SupportsNegativePrompt = false,
                    SupportsSeed = false,
                    SupportsStyle = true,
                    SupportsGuidanceScale = false,
                    SupportsSteps = false,
                    SupportsBatchGeneration = false
                }
            },
            new ImageGenerationModel
            {
                Id = "dall-e-2",
                Name = "DALL-E 2",
                Description = "OpenAI's previous generation image model",
                IsAvailable = true,
                SupportedDimensions = new List<ImageDimension>
                {
                    new() { Width = 256, Height = 256, AspectRatio = "1:1" },
                    new() { Width = 512, Height = 512, AspectRatio = "1:1" },
                    new() { Width = 1024, Height = 1024, AspectRatio = "1:1" }
                },
                MaxImagesPerRequest = 10,
                MaxPromptLength = 1000,
                Capabilities = new ImageGenerationCapabilities
                {
                    SupportsNegativePrompt = false,
                    SupportsSeed = true,
                    SupportsStyle = false,
                    SupportsGuidanceScale = false,
                    SupportsSteps = false,
                    SupportsBatchGeneration = true
                }
            }
        });
    }

    private string GetSizeString(int width, int height)
    {
        return (width, height) switch
        {
            (1024, 1024) => "1024x1024",
            (1024, 1792) => "1024x1792",
            (1792, 1024) => "1792x1024",
            (256, 256) => "256x256",
            (512, 512) => "512x512",
            _ => "1024x1024" // Default fallback
        };
    }

    private decimal? CalculateCost(string modelId, int imageCount)
    {
        return modelId switch
        {
            "dall-e-3" => imageCount * 0.040m, // $0.040 per image
            "dall-e-2" => imageCount * 0.020m, // $0.020 per image
            _ => null
        };
    }

    private async Task<string?> SaveImageToFileAsync(byte[] imageData, string prompt)
    {
        try
        {
            var fileName = $"openai_generated_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.png";
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

/// <summary>
/// OpenAI API response models
/// </summary>
internal record OpenAIErrorResponse
{
    public OpenAIError? Error { get; init; }
}

internal record OpenAIError
{
    public string? Message { get; init; }
    public string? Type { get; init; }
}

internal record OpenAIImageGenerationResponse
{
    public List<OpenAIImageData>? Data { get; init; }
}

internal record OpenAIImageData
{
    public string? Url { get; init; }
    public string? B64Json { get; init; }
}
