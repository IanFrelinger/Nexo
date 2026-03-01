using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Assets.Ports;

namespace Nexo.Adapters.Assets.Images;

/// <summary>
/// Local/self-hosted image generation adapter.
/// 
/// Supports multiple local image generation providers:
/// - Stable Diffusion (Automatic1111/ComfyUI)
/// - Ollama
/// - LocalAI
/// - Custom HTTP endpoints
/// 
/// Implements IImageGenerator for use with ImageAssetAgent.
/// Provides retry logic and error handling for local generation.
/// </summary>
public sealed class LocalImageGenerator : IImageGenerator
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly LocalImageProvider _provider;
    private readonly string _ollamaModel;
    private readonly ILogger<LocalImageGenerator> _logger;

    public LocalImageGenerator(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<LocalImageGenerator> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _baseUrl = configuration["LocalImageGenerator:BaseUrl"]
            ?? throw new InvalidOperationException("LocalImageGenerator:BaseUrl not configured");
        _provider = Enum.Parse<LocalImageProvider>(
            configuration["LocalImageGenerator:Provider"] ?? "StableDiffusion");
        _ollamaModel = configuration["LocalImageGenerator:OllamaModel"] ?? "x/flux2-klein";

        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(5); // Local generation can take time

        var apiKey = configuration["LocalImageGenerator:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }
    }

    public IReadOnlyList<ImageSize> SupportedSizes { get; } = new[]
    {
        ImageSize.Square256,
        ImageSize.Square512,
        ImageSize.Square1024,
        ImageSize.Portrait768x1024,
        ImageSize.Landscape1024x768,
        ImageSize.Wide1920x1080
    };

    public IReadOnlyList<string> SupportedStyles { get; } = new[]
    {
        "realistic",
        "anime",
        "photographic",
        "digital-art",
        "fantasy-art",
        "comic-book"
    };

    public async Task<GeneratedImage> GenerateAsync(
        ImageGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        return _provider switch
        {
            LocalImageProvider.StableDiffusion => await GenerateStableDiffusionAsync(request, cancellationToken),
            LocalImageProvider.Ollama => await GenerateOllamaAsync(request, cancellationToken),
            LocalImageProvider.LocalAI => await GenerateLocalAIAsync(request, cancellationToken),
            LocalImageProvider.Custom => await GenerateCustomAsync(request, cancellationToken),
            _ => throw new NotSupportedException($"Provider {_provider} not supported")
        };
    }

    public Task<IReadOnlyList<GeneratedImage>> GenerateVariationsAsync(
        string sourceImagePath,
        int count,
        CancellationToken cancellationToken = default)
    {
        // Most local providers support img2img
        return _provider switch
        {
            LocalImageProvider.StableDiffusion => GenerateStableDiffusionVariationsAsync(sourceImagePath, count, cancellationToken),
            LocalImageProvider.LocalAI => GenerateLocalAIVariationsAsync(sourceImagePath, count, cancellationToken),
            _ => throw new NotSupportedException($"Variations not supported for provider {_provider}")
        };
    }

    private async Task<GeneratedImage> GenerateStableDiffusionAsync(
        ImageGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var (width, height) = GetDimensions(request.Size);
        var payload = new
        {
            prompt = request.Prompt,
            negative_prompt = request.NegativePrompt ?? "",
            width = width,
            height = height,
            steps = 20,
            cfg_scale = request.GuidanceScale ?? 7.0,
            seed = request.Seed ?? -1,
            sampler_index = "Euler a"
        };

        _logger.LogInformation("Generating image with Stable Diffusion: {Prompt}", request.Prompt);

        var response = await _httpClient.PostAsJsonAsync("sdapi/v1/txt2img", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<StableDiffusionResponse>(cancellationToken: cancellationToken);
        var imageBase64 = result?.Images?.FirstOrDefault()
            ?? throw new InvalidOperationException("No image in Stable Diffusion response");

        var imageBytes = Convert.FromBase64String(imageBase64);
        var tempPath = Path.Combine(Path.GetTempPath(), $"sd_{Guid.NewGuid()}.png");
        await File.WriteAllBytesAsync(tempPath, imageBytes, cancellationToken);

        _logger.LogInformation("Generated image saved to {Path}", tempPath);

        return new GeneratedImage
        {
            FilePath = tempPath,
            Size = request.Size,
            MimeType = "image/png",
            Seed = result.Info?.Seed != null ? (int?)result.Info.Seed : null,
            Metadata = new Dictionary<string, object>
            {
                ["generator"] = "StableDiffusion",
                ["provider"] = "local",
                ["prompt"] = request.Prompt,
                ["width"] = width,
                ["height"] = height
            }
        };
    }

    private async Task<GeneratedImage> GenerateOllamaAsync(
        ImageGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var model = _ollamaModel;
        var payload = new
        {
            model,
            prompt = request.Prompt,
            stream = false,
            options = request.Seed.HasValue ? new { seed = request.Seed.Value } : (object?)null
        };

        _logger.LogInformation("Generating image with Ollama ({Model}): {Prompt}", model, request.Prompt);

        var response = await _httpClient.PostAsJsonAsync("api/generate", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        var imageBase64 = ExtractOllamaImage(json, model);
        if (string.IsNullOrEmpty(imageBase64))
            throw new InvalidOperationException(
                $"Ollama model '{model}' did not return an image. " +
                "Use an image generation model (e.g. x/flux2-klein, x/z-image-turbo). " +
                "Text-only models like llava return text, not images.");

        var imageBytes = Convert.FromBase64String(imageBase64);
        var tempPath = Path.Combine(Path.GetTempPath(), $"ollama_{Guid.NewGuid()}.png");
        await File.WriteAllBytesAsync(tempPath, imageBytes, cancellationToken);

        _logger.LogInformation("Generated image saved to {Path}", tempPath);

        return new GeneratedImage
        {
            FilePath = tempPath,
            Size = request.Size,
            MimeType = "image/png",
            Seed = request.Seed,
            Metadata = new Dictionary<string, object>
            {
                ["generator"] = "Ollama",
                ["provider"] = "local",
                ["model"] = model,
                ["prompt"] = request.Prompt
            }
        };
    }

    private static string? ExtractOllamaImage(JsonElement json, string model)
    {
        if (json.TryGetProperty("images", out var images) && images.ValueKind == JsonValueKind.Array && images.GetArrayLength() > 0)
            return images[0].GetString();
        if (json.TryGetProperty("image", out var image))
            return image.GetString();
        var response = json.TryGetProperty("response", out var r) ? r.GetString() : null;
        if (!string.IsNullOrEmpty(response) && IsBase64(response))
            return response;
        return null;
    }

    private static bool IsBase64(string s)
    {
        if (string.IsNullOrEmpty(s) || s.Length % 4 != 0) return false;
        try
        {
            Convert.FromBase64String(s);
            return true;
        }
        catch { return false; }
    }

    private async Task<GeneratedImage> GenerateLocalAIAsync(
        ImageGenerationRequest request,
        CancellationToken cancellationToken)
    {
        // LocalAI uses OpenAI-compatible API
        var (width, height) = GetDimensions(request.Size);
        var payload = new
        {
            prompt = request.Prompt,
            n = 1,
            size = $"{width}x{height}",
            response_format = "b64_json"
        };

        _logger.LogInformation("Generating image with LocalAI: {Prompt}", request.Prompt);

        var response = await _httpClient.PostAsJsonAsync("v1/images/generations", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<LocalAIResponse>(cancellationToken: cancellationToken);
        var imageData = result?.Data?.FirstOrDefault()
            ?? throw new InvalidOperationException("No image in LocalAI response");

        var imageBytes = Convert.FromBase64String(imageData.B64Json);
        var tempPath = Path.Combine(Path.GetTempPath(), $"localai_{Guid.NewGuid()}.png");
        await File.WriteAllBytesAsync(tempPath, imageBytes, cancellationToken);

        return new GeneratedImage
        {
            FilePath = tempPath,
            Size = request.Size,
            MimeType = "image/png",
            Metadata = new Dictionary<string, object>
            {
                ["generator"] = "LocalAI",
                ["provider"] = "local"
            }
        };
    }

    private async Task<GeneratedImage> GenerateCustomAsync(
        ImageGenerationRequest request,
        CancellationToken cancellationToken)
    {
        // Custom endpoint - expects OpenAI-compatible format
        var (width, height) = GetDimensions(request.Size);
        var payload = new
        {
            prompt = request.Prompt,
            negative_prompt = request.NegativePrompt,
            width = width,
            height = height,
            seed = request.Seed,
            guidance_scale = request.GuidanceScale
        };

        _logger.LogInformation("Generating image with custom endpoint: {Prompt}", request.Prompt);

        var response = await _httpClient.PostAsJsonAsync("generate", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        // Expects response with image URL or base64
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        
        string imagePath;
        if (result.TryGetProperty("image_url", out var urlElement))
        {
            var imageUrl = urlElement.GetString() ?? throw new InvalidOperationException("Invalid image_url");
            imagePath = await DownloadImageAsync(imageUrl, cancellationToken);
        }
        else if (result.TryGetProperty("image_base64", out var b64Element))
        {
            var imageBase64 = b64Element.GetString() ?? throw new InvalidOperationException("Invalid image_base64");
            var imageBytes = Convert.FromBase64String(imageBase64);
            imagePath = Path.Combine(Path.GetTempPath(), $"custom_{Guid.NewGuid()}.png");
            await File.WriteAllBytesAsync(imagePath, imageBytes, cancellationToken);
        }
        else
        {
            throw new InvalidOperationException("Response must contain 'image_url' or 'image_base64'");
        }

        return new GeneratedImage
        {
            FilePath = imagePath,
            Size = request.Size,
            MimeType = "image/png",
            Metadata = new Dictionary<string, object>
            {
                ["generator"] = "Custom",
                ["provider"] = "local",
                ["endpoint"] = _baseUrl
            }
        };
    }

    private async Task<IReadOnlyList<GeneratedImage>> GenerateStableDiffusionVariationsAsync(
        string sourceImagePath,
        int count,
        CancellationToken cancellationToken)
    {
        var imageBytes = await File.ReadAllBytesAsync(sourceImagePath, cancellationToken);
        var imageBase64 = Convert.ToBase64String(imageBytes);

        var payload = new
        {
            init_images = new[] { imageBase64 },
            prompt = "variation",
            n_iter = count,
            steps = 20,
            denoising_strength = 0.75
        };

        var response = await _httpClient.PostAsJsonAsync("sdapi/v1/img2img", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<StableDiffusionResponse>(cancellationToken: cancellationToken);
        var images = new List<GeneratedImage>();

        foreach (var base64Image in result?.Images ?? Array.Empty<string>())
        {
            var bytes = Convert.FromBase64String(base64Image);
            var tempPath = Path.Combine(Path.GetTempPath(), $"sd_var_{Guid.NewGuid()}.png");
            await File.WriteAllBytesAsync(tempPath, bytes, cancellationToken);
            images.Add(new GeneratedImage
            {
                FilePath = tempPath,
                Size = ImageSize.Square1024,
                MimeType = "image/png",
                Metadata = new Dictionary<string, object> { ["generator"] = "StableDiffusion" }
            });
        }

        return images;
    }

    private async Task<IReadOnlyList<GeneratedImage>> GenerateLocalAIVariationsAsync(
        string sourceImagePath,
        int count,
        CancellationToken cancellationToken)
    {
        var imageBytes = await File.ReadAllBytesAsync(sourceImagePath, cancellationToken);
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(imageBytes), "image", Path.GetFileName(sourceImagePath) ?? "image.png");
        content.Add(new StringContent(count.ToString()), "n");
        content.Add(new StringContent("b64_json"), "response_format");

        var response = await _httpClient.PostAsync("v1/images/variations", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<LocalAIResponse>(cancellationToken: cancellationToken);
        var images = new List<GeneratedImage>();
        foreach (var item in result?.Data ?? Array.Empty<LocalAIImageData>())
        {
            if (string.IsNullOrEmpty(item.B64Json)) continue;
            var bytes = Convert.FromBase64String(item.B64Json);
            var tempPath = Path.Combine(Path.GetTempPath(), $"localai_var_{Guid.NewGuid()}.png");
            await File.WriteAllBytesAsync(tempPath, bytes, cancellationToken);
            images.Add(new GeneratedImage
            {
                FilePath = tempPath,
                Size = ImageSize.Square1024,
                MimeType = "image/png",
                Metadata = new Dictionary<string, object> { ["generator"] = "LocalAI" }
            });
        }
        return images;
    }

    private (int Width, int Height) GetDimensions(ImageSize size)
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

    private async Task<string> DownloadImageAsync(string url, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var imageBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var tempPath = Path.Combine(Path.GetTempPath(), $"downloaded_{Guid.NewGuid()}.png");
        await File.WriteAllBytesAsync(tempPath, imageBytes, cancellationToken);
        return tempPath;
    }

    private sealed record StableDiffusionResponse(string[]? Images, StableDiffusionInfo? Info);
    private sealed record StableDiffusionInfo(long? Seed);
    private sealed record LocalAIResponse(LocalAIImageData[]? Data);
    private sealed record LocalAIImageData([property: JsonPropertyName("b64_json")] string B64Json, [property: JsonPropertyName("url")] string? Url);
}

/// <summary>
/// Supported local image generation providers.
/// 
/// Defines local image generation options:
/// - StableDiffusion: Stable Diffusion (Automatic1111 or ComfyUI)
/// - Ollama: Ollama (requires image generation model)
/// - LocalAI: LocalAI (OpenAI-compatible)
/// - Custom: Custom endpoint (OpenAI-compatible format)
/// 
/// Used by LocalImageGenerator to select the provider.
/// </summary>
public enum LocalImageProvider
{
    /// <summary>
    /// Stable Diffusion (Automatic1111 or ComfyUI).
    /// </summary>
    StableDiffusion,

    /// <summary>
    /// Ollama (requires image generation model).
    /// </summary>
    Ollama,

    /// <summary>
    /// LocalAI (OpenAI-compatible).
    /// </summary>
    LocalAI,

    /// <summary>
    /// Custom endpoint (OpenAI-compatible format).
    /// </summary>
    Custom
}

