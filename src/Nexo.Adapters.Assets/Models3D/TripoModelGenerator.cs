using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Assets.Ports;

namespace Nexo.Adapters.Assets.Models3D;

/// <summary>
/// Tripo AI 3D model generation implementation.
/// </summary>
public sealed class TripoModelGenerator : IModel3DGenerator
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<TripoModelGenerator> _logger;

    public TripoModelGenerator(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<TripoModelGenerator> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _apiKey = configuration["Tripo:ApiKey"]
            ?? throw new InvalidOperationException("Tripo API key not configured");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _httpClient.BaseAddress = new Uri("https://api.tripo3d.ai/v1/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<Generated3DModel> GenerateFromTextAsync(
        Model3DGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            prompt = request.Prompt,
            negative_prompt = "",
            seed = 0,
            guidance_scale = 7.5,
            num_inference_steps = 50,
            resolution = MapQualityToResolution(request.Quality)
        };

        _logger.LogInformation("Starting Tripo text-to-3D generation: {Prompt}", request.Prompt);

        var createResponse = await _httpClient.PostAsJsonAsync("text-to-3d", payload, cancellationToken);
        createResponse.EnsureSuccessStatusCode();

        var createResult = await createResponse.Content.ReadFromJsonAsync<TripoCreateResponse>(cancellationToken: cancellationToken);
        var taskId = createResult?.TaskId
            ?? throw new InvalidOperationException("No task ID in Tripo response");

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

        // Upload image
        await using var imageStream = File.OpenRead(imagePath);
        var imageContent = new StreamContent(imageStream);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        var uploadResponse = await _httpClient.PostAsync("image-to-3d/upload", imageContent, cancellationToken);
        uploadResponse.EnsureSuccessStatusCode();

        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<TripoUploadResponse>(cancellationToken: cancellationToken);
        var imageId = uploadResult?.ImageId
            ?? throw new InvalidOperationException("No image ID in upload response");

        // Start image-to-3D generation
        var payload = new
        {
            image_id = imageId,
            prompt = request.Prompt,
            negative_prompt = "",
            seed = 0,
            resolution = MapQualityToResolution(request.Quality)
        };

        _logger.LogInformation("Starting Tripo image-to-3D generation from: {ImagePath}", imagePath);

        var createResponse = await _httpClient.PostAsJsonAsync("image-to-3d", payload, cancellationToken);
        createResponse.EnsureSuccessStatusCode();

        var createResult = await createResponse.Content.ReadFromJsonAsync<TripoCreateResponse>(cancellationToken: cancellationToken);
        var taskId = createResult?.TaskId
            ?? throw new InvalidOperationException("No task ID in Tripo response");

        // Poll for completion
        var model = await PollForCompletionAsync(taskId, request, cancellationToken);

        return model;
    }

    private async Task<Generated3DModel> PollForCompletionAsync(
        string taskId,
        Model3DGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var maxAttempts = 60; // 5 minutes max
        var attempt = 0;

        while (attempt < maxAttempts)
        {
            await Task.Delay(5000, cancellationToken);

            var statusResponse = await _httpClient.GetAsync($"tasks/{taskId}", cancellationToken);
            statusResponse.EnsureSuccessStatusCode();

            var statusResult = await statusResponse.Content.ReadFromJsonAsync<TripoStatusResponse>(cancellationToken: cancellationToken);

            if (statusResult?.Status == "completed")
            {
                var modelUrl = statusResult.ModelUrl
                    ?? throw new InvalidOperationException("No model URL in completed response");

                // Download model
                var extension = request.OutputFormat switch
                {
                    Model3DFormat.GLB => ".glb",
                    Model3DFormat.GLTF => ".gltf",
                    Model3DFormat.FBX => ".fbx",
                    Model3DFormat.OBJ => ".obj",
                    Model3DFormat.USD => ".usd",
                    _ => ".glb"
                };

                var tempPath = Path.Combine(Path.GetTempPath(), $"tripo_{Guid.NewGuid()}{extension}");
                await using var modelStream = await _httpClient.GetStreamAsync(modelUrl, cancellationToken);
                await using var fileStream = File.Create(tempPath);
                await modelStream.CopyToAsync(fileStream, cancellationToken);

                // Download textures
                var texturePaths = new List<string>();
                if (request.GenerateTextures && statusResult.TextureUrls != null)
                {
                    foreach (var textureUrl in statusResult.TextureUrls)
                    {
                        var texturePath = Path.Combine(Path.GetTempPath(), $"tripo_texture_{Guid.NewGuid()}.png");
                        await using var textureStream = await _httpClient.GetStreamAsync(textureUrl, cancellationToken);
                        await using var textureFileStream = File.Create(texturePath);
                        await textureStream.CopyToAsync(textureFileStream, cancellationToken);
                        texturePaths.Add(texturePath);
                    }
                }

                _logger.LogInformation("Generated 3D model saved to {Path}", tempPath);

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
                        ["generator"] = "Tripo",
                        ["taskId"] = taskId,
                        ["prompt"] = request.Prompt,
                        ["quality"] = request.Quality.ToString()
                    }
                };
            }

            if (statusResult?.Status == "failed")
            {
                throw new InvalidOperationException($"Tripo generation failed: {statusResult.Error ?? "Unknown error"}");
            }

            attempt++;
        }

        throw new TimeoutException($"Tripo generation timed out after {maxAttempts * 5} seconds");
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

    private sealed record TripoCreateResponse(string TaskId);
    private sealed record TripoUploadResponse(string ImageId);
    private sealed record TripoStatusResponse(string Status, string? ModelUrl, string[]? TextureUrls, string? Error);
}

