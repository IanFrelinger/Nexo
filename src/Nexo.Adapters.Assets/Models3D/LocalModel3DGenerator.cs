using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Assets.Ports;

namespace Nexo.Adapters.Assets.Models3D;

/// <summary>
/// Local/self-hosted 3D model generation adapter.
/// Supports custom endpoints and local 3D generation services.
/// </summary>
public sealed class LocalModel3DGenerator : IModel3DGenerator
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly ILogger<LocalModel3DGenerator> _logger;

    public LocalModel3DGenerator(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<LocalModel3DGenerator> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _baseUrl = configuration["LocalModel3DGenerator:BaseUrl"]
            ?? throw new InvalidOperationException("LocalModel3DGenerator:BaseUrl not configured");

        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(30); // 3D generation can take a long time

        var apiKey = configuration["LocalModel3DGenerator:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }
    }

    public async Task<Generated3DModel> GenerateFromTextAsync(
        Model3DGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            prompt = request.Prompt,
            format = request.OutputFormat.ToString().ToLowerInvariant(),
            quality = request.Quality.ToString().ToLowerInvariant(),
            generate_textures = request.GenerateTextures,
            target_poly_count = request.TargetPolyCount
        };

        _logger.LogInformation("Starting local text-to-3D generation: {Prompt}", request.Prompt);

        var response = await _httpClient.PostAsJsonAsync("text-to-3d", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        
        // Check if it's async (task ID) or sync (direct model)
        if (result.TryGetProperty("task_id", out var taskIdElement))
        {
            var taskId = taskIdElement.GetString()!;
            return await PollForCompletionAsync(taskId, request, cancellationToken);
        }
        else
        {
            // Synchronous response
            return await ParseModelResponseAsync(result, request, cancellationToken);
        }
    }

    public async Task<Generated3DModel> GenerateFromImageAsync(
        string imagePath,
        Model3DGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException($"Reference image not found: {imagePath}");
        }

        // Upload image
        await using var imageStream = File.OpenRead(imagePath);
        var imageContent = new StreamContent(imageStream);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        var multipart = new MultipartFormDataContent
        {
            { imageContent, "image", Path.GetFileName(imagePath) },
            { new StringContent(request.Prompt), "prompt" },
            { new StringContent(request.OutputFormat.ToString().ToLowerInvariant()), "format" },
            { new StringContent(request.Quality.ToString().ToLowerInvariant()), "quality" },
            { new StringContent(request.GenerateTextures.ToString()), "generate_textures" }
        };

        _logger.LogInformation("Starting local image-to-3D generation from: {ImagePath}", imagePath);

        var response = await _httpClient.PostAsync("image-to-3d", multipart, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        
        if (result.TryGetProperty("task_id", out var taskIdElement))
        {
            var taskId = taskIdElement.GetString()!;
            return await PollForCompletionAsync(taskId, request, cancellationToken);
        }
        else
        {
            return await ParseModelResponseAsync(result, request, cancellationToken);
        }
    }

    private async Task<Generated3DModel> PollForCompletionAsync(
        string taskId,
        Model3DGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var maxAttempts = 120; // 10 minutes max (5 second intervals)
        var attempt = 0;

        while (attempt < maxAttempts)
        {
            await Task.Delay(5000, cancellationToken);

            var statusResponse = await _httpClient.GetAsync($"tasks/{taskId}", cancellationToken);
            statusResponse.EnsureSuccessStatusCode();

            var statusResult = await statusResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            
            var status = statusResult.TryGetProperty("status", out var statusElement)
                ? statusElement.GetString()
                : "unknown";

            if (status == "completed" || status == "success")
            {
                return await ParseModelResponseAsync(statusResult, request, cancellationToken);
            }

            if (status == "failed" || status == "error")
            {
                var error = statusResult.TryGetProperty("error", out var errorElement)
                    ? errorElement.GetString()
                    : "Unknown error";
                throw new InvalidOperationException($"Local 3D generation failed: {error}");
            }

            attempt++;
        }

        throw new TimeoutException($"Local 3D generation timed out after {maxAttempts * 5} seconds");
    }

    private async Task<Generated3DModel> ParseModelResponseAsync(
        JsonElement result,
        Model3DGenerationRequest request,
        CancellationToken cancellationToken)
    {
        string modelPath;
        var texturePaths = new List<string>();

        // Try different response formats
        if (result.TryGetProperty("model_url", out var modelUrlElement))
        {
            var modelUrl = modelUrlElement.GetString()!;
            modelPath = await DownloadFileAsync(modelUrl, request.OutputFormat, cancellationToken);
        }
        else if (result.TryGetProperty("model_path", out var modelPathElement))
        {
            modelPath = modelPathElement.GetString()!;
            if (!File.Exists(modelPath))
            {
                throw new FileNotFoundException($"Model file not found: {modelPath}");
            }
        }
        else if (result.TryGetProperty("model_base64", out var modelB64Element))
        {
            var modelBytes = Convert.FromBase64String(modelB64Element.GetString()!);
            var extension = GetExtension(request.OutputFormat);
            modelPath = Path.Combine(Path.GetTempPath(), $"local_model_{Guid.NewGuid()}{extension}");
            await File.WriteAllBytesAsync(modelPath, modelBytes, cancellationToken);
        }
        else
        {
            throw new InvalidOperationException("Response must contain 'model_url', 'model_path', or 'model_base64'");
        }

        // Handle textures
        if (request.GenerateTextures)
        {
            if (result.TryGetProperty("texture_urls", out var textureUrlsElement))
            {
                foreach (var textureUrl in textureUrlsElement.EnumerateArray())
                {
                    var texturePath = await DownloadFileAsync(textureUrl.GetString()!, ".png", cancellationToken);
                    texturePaths.Add(texturePath);
                }
            }
            else if (result.TryGetProperty("texture_paths", out var texturePathsElement))
            {
                foreach (var texturePath in texturePathsElement.EnumerateArray())
                {
                    var path = texturePath.GetString()!;
                    if (File.Exists(path))
                    {
                        texturePaths.Add(path);
                    }
                }
            }
        }

        // Estimate polygon count
        var (vertexCount, triangleCount) = EstimatePolyCount(request.Quality);

        _logger.LogInformation("Generated 3D model saved to {Path}", modelPath);

        return new Generated3DModel
        {
            FilePath = modelPath,
            Format = request.OutputFormat,
            VertexCount = vertexCount,
            TriangleCount = triangleCount,
            TexturePaths = texturePaths,
            Metadata = new Dictionary<string, object>
            {
                ["generator"] = "Local",
                ["provider"] = "local",
                ["prompt"] = request.Prompt,
                ["quality"] = request.Quality.ToString()
            }
        };
    }

    private async Task<string> DownloadFileAsync(string url, Model3DFormat format, CancellationToken cancellationToken)
    {
        var extension = GetExtension(format);
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var tempPath = Path.Combine(Path.GetTempPath(), $"local_model_{Guid.NewGuid()}{extension}");
        await File.WriteAllBytesAsync(tempPath, bytes, cancellationToken);
        return tempPath;
    }

    private async Task<string> DownloadFileAsync(string url, string extension, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var tempPath = Path.Combine(Path.GetTempPath(), $"local_texture_{Guid.NewGuid()}{extension}");
        await File.WriteAllBytesAsync(tempPath, bytes, cancellationToken);
        return tempPath;
    }

    private string GetExtension(Model3DFormat format)
    {
        return format switch
        {
            Model3DFormat.GLB => ".glb",
            Model3DFormat.GLTF => ".gltf",
            Model3DFormat.FBX => ".fbx",
            Model3DFormat.OBJ => ".obj",
            Model3DFormat.USD => ".usd",
            _ => ".glb"
        };
    }

    private (int VertexCount, int TriangleCount) EstimatePolyCount(ModelQuality quality)
    {
        return quality switch
        {
            ModelQuality.Draft => (500, 1000),
            ModelQuality.Low => (2000, 4000),
            ModelQuality.Medium => (5000, 10000),
            ModelQuality.High => (10000, 20000),
            ModelQuality.Production => (20000, 40000),
            _ => (5000, 10000)
        };
    }
}

