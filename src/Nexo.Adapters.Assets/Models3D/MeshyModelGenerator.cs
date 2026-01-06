using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Assets.Ports;

namespace Nexo.Adapters.Assets.Models3D;

/// <summary>
/// Meshy AI 3D model generation implementation.
/// 
/// Provides:
/// - Text-to-3D and image-to-3D generation via Meshy AI API
/// - Multiple quality levels and art styles
/// - Async generation with polling
/// - Model download and local storage
/// 
/// Implements IModel3DGenerator for use with Model3DAssetAgent.
/// Requires Meshy API key configuration.
/// </summary>
public sealed class MeshyModelGenerator : IModel3DGenerator
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<MeshyModelGenerator> _logger;

    public MeshyModelGenerator(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<MeshyModelGenerator> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _apiKey = configuration["Meshy:ApiKey"]
            ?? throw new InvalidOperationException("Meshy API key not configured");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _httpClient.BaseAddress = new Uri("https://api.meshy.ai/v2/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<Generated3DModel> GenerateFromTextAsync(
        Model3DGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        // Start text-to-3D generation
        var payload = new
        {
            mode = 1, // Text-to-3D mode
            prompt = request.Prompt,
            art_style = "realistic",
            negative_prompt = "",
            seed = 0,
            resolution = MapQualityToResolution(request.Quality)
        };

        _logger.LogInformation("Starting Meshy text-to-3D generation: {Prompt}", request.Prompt);

        var createResponse = await _httpClient.PostAsJsonAsync("text-to-3d", payload, cancellationToken);
        createResponse.EnsureSuccessStatusCode();

        var createResult = await createResponse.Content.ReadFromJsonAsync<MeshyCreateResponse>(cancellationToken: cancellationToken);
        var taskId = createResult?.Result 
            ?? throw new InvalidOperationException("No task ID in Meshy response");

        // Poll for completion
        var model = await PollForCompletionAsync(taskId, request, cancellationToken);

        return model;
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

        // Upload image first
        await using var imageStream = File.OpenRead(imagePath);
        var imageContent = new StreamContent(imageStream);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        var uploadResponse = await _httpClient.PostAsync("image-to-3d/upload", imageContent, cancellationToken);
        uploadResponse.EnsureSuccessStatusCode();

        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<MeshyUploadResponse>(cancellationToken: cancellationToken);
        var imageUrl = uploadResult?.Result?.ImageUrl
            ?? throw new InvalidOperationException("No image URL in upload response");

        // Start image-to-3D generation
        var payload = new
        {
            image_url = imageUrl,
            art_style = "realistic",
            negative_prompt = "",
            seed = 0,
            resolution = MapQualityToResolution(request.Quality)
        };

        _logger.LogInformation("Starting Meshy image-to-3D generation from: {ImagePath}", imagePath);

        var createResponse = await _httpClient.PostAsJsonAsync("image-to-3d", payload, cancellationToken);
        createResponse.EnsureSuccessStatusCode();

        var createResult = await createResponse.Content.ReadFromJsonAsync<MeshyCreateResponse>(cancellationToken: cancellationToken);
        var taskId = createResult?.Result
            ?? throw new InvalidOperationException("No task ID in Meshy response");

        // Poll for completion
        var model = await PollForCompletionAsync(taskId, request, cancellationToken);

        return model;
    }

    private async Task<Generated3DModel> PollForCompletionAsync(
        string taskId,
        Model3DGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var maxAttempts = 60; // 5 minutes max (5 second intervals)
        var attempt = 0;

        while (attempt < maxAttempts)
        {
            await Task.Delay(5000, cancellationToken); // Poll every 5 seconds

            var statusResponse = await _httpClient.GetAsync($"text-to-3d/{taskId}", cancellationToken);
            statusResponse.EnsureSuccessStatusCode();

            var statusResult = await statusResponse.Content.ReadFromJsonAsync<MeshyStatusResponse>(cancellationToken: cancellationToken);
            
            if (statusResult?.Status == "COMPLETED")
            {
                var modelUrl = statusResult.ModelUrls?.FirstOrDefault()
                    ?? throw new InvalidOperationException("No model URL in completed response");

                // Download model to local file
                var extension = request.OutputFormat switch
                {
                    Model3DFormat.GLB => ".glb",
                    Model3DFormat.GLTF => ".gltf",
                    Model3DFormat.FBX => ".fbx",
                    Model3DFormat.OBJ => ".obj",
                    Model3DFormat.USD => ".usd",
                    _ => ".glb"
                };

                var tempPath = Path.Combine(Path.GetTempPath(), $"meshy_{Guid.NewGuid()}{extension}");
                await using var modelStream = await _httpClient.GetStreamAsync(modelUrl, cancellationToken);
                await using var fileStream = File.Create(tempPath);
                await modelStream.CopyToAsync(fileStream, cancellationToken);

                // Download textures if available
                var texturePaths = new List<string>();
                if (request.GenerateTextures && statusResult.TextureUrls != null)
                {
                    foreach (var textureUrl in statusResult.TextureUrls)
                    {
                        var texturePath = Path.Combine(Path.GetTempPath(), $"meshy_texture_{Guid.NewGuid()}.png");
                        await using var textureStream = await _httpClient.GetStreamAsync(textureUrl, cancellationToken);
                        await using var textureFileStream = File.Create(texturePath);
                        await textureStream.CopyToAsync(textureFileStream, cancellationToken);
                        texturePaths.Add(texturePath);
                    }
                }

                _logger.LogInformation("Generated 3D model saved to {Path}", tempPath);

                // Estimate polygon count based on quality
                var (vertexCount, triangleCount) = EstimatePolyCount(request.Quality);

                return new Generated3DModel
                {
                    FilePath = tempPath,
                    Format = request.OutputFormat,
                    VertexCount = vertexCount,
                    TriangleCount = triangleCount,
                    TexturePaths = texturePaths,
                    Metadata = new Dictionary<string, object>
                    {
                        ["generator"] = "Meshy",
                        ["taskId"] = taskId,
                        ["prompt"] = request.Prompt,
                        ["quality"] = request.Quality.ToString()
                    }
                };
            }

            if (statusResult?.Status == "FAILED")
            {
                throw new InvalidOperationException($"Meshy generation failed: {statusResult.Error ?? "Unknown error"}");
            }

            attempt++;
        }

        throw new TimeoutException($"Meshy generation timed out after {maxAttempts * 5} seconds");
    }

    private string MapQualityToResolution(ModelQuality quality)
    {
        return quality switch
        {
            ModelQuality.Draft => "256",
            ModelQuality.Low => "512",
            ModelQuality.Medium => "1024",
            ModelQuality.High => "2048",
            ModelQuality.Production => "4096",
            _ => "1024"
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

    private sealed record MeshyCreateResponse(string Result);
    private sealed record MeshyUploadResponse(MeshyUploadResult? Result);
    private sealed record MeshyUploadResult(string ImageUrl);
    private sealed record MeshyStatusResponse(string Status, string[]? ModelUrls, string[]? TextureUrls, string? Error);
}

