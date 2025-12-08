using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Assets.Ports;

namespace Nexo.Adapters.Assets.Images;

/// <summary>
/// DALL-E image generator implementation using OpenAI API.
/// </summary>
public sealed class DalleImageGenerator : IImageGenerator
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<DalleImageGenerator> _logger;

    public IReadOnlyList<ImageSize> SupportedSizes => new[]
    {
        ImageSize.Square1024,
        ImageSize.Portrait768x1024,
        ImageSize.Landscape1024x768
    };

    public IReadOnlyList<string> SupportedStyles => new[]
    {
        "vivid",
        "natural"
    };

    public DalleImageGenerator(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<DalleImageGenerator> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _apiKey = configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI API key not configured");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GeneratedImage> GenerateAsync(
        ImageGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            model = "dall-e-3",
            prompt = request.Prompt,
            n = 1,
            size = MapSize(request.Size),
            quality = "hd",
            style = request.Style ?? "vivid",
            response_format = "url"
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/images/generations")
        {
            Content = JsonContent.Create(payload)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        _logger.LogInformation("Generating image with DALL-E 3: {Prompt}", request.Prompt);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<DalleResponse>(cancellationToken: cancellationToken);
        var imageUrl = result?.Data?.FirstOrDefault()?.Url
            ?? throw new InvalidOperationException("No image URL in response");

        // Download image to local file
        var localPath = Path.Combine(Path.GetTempPath(), $"dalle_{Guid.NewGuid()}.png");
        await using var imageStream = await _httpClient.GetStreamAsync(imageUrl, cancellationToken);
        await using var fileStream = File.Create(localPath);
        await imageStream.CopyToAsync(fileStream, cancellationToken);

        _logger.LogInformation("Generated image saved to {Path}", localPath);

        return new GeneratedImage
        {
            FilePath = localPath,
            Size = request.Size,
            MimeType = "image/png",
            Seed = request.Seed,
            Metadata = new Dictionary<string, object>
            {
                ["revised_prompt"] = result?.Data?.FirstOrDefault()?.RevisedPrompt ?? request.Prompt,
                ["generator"] = "DALL-E-3",
                ["model"] = "dall-e-3"
            }
        };
    }

    public Task<IReadOnlyList<GeneratedImage>> GenerateVariationsAsync(
        string sourceImagePath,
        int count,
        CancellationToken cancellationToken = default)
    {
        // DALL-E 3 doesn't support variations directly
        // Would need to use DALL-E 2 or implement via describe + regenerate
        _logger.LogWarning("Variations not supported with DALL-E 3. Use DALL-E 2 or implement describe+regenerate pattern.");
        return Task.FromException<IReadOnlyList<GeneratedImage>>(
            new NotSupportedException("Variations not supported with DALL-E 3. Use DALL-E 2 API or implement describe+regenerate pattern."));
    }

    private string MapSize(ImageSize size) => size switch
    {
        ImageSize.Square1024 => "1024x1024",
        ImageSize.Portrait768x1024 => "1024x1792",
        ImageSize.Landscape1024x768 => "1792x1024",
        _ => "1024x1024"
    };

    private sealed record DalleResponse(DalleImage[] Data);
    private sealed record DalleImage(string Url, string RevisedPrompt);
}

