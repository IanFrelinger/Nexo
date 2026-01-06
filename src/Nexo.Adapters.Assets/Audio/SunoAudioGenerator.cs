using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Assets.Ports;

namespace Nexo.Adapters.Assets.Audio;

/// <summary>
/// Suno AI music generation implementation.
/// 
/// Provides:
/// - Music and sound effect generation via Suno AI API
/// - Genre and BPM parameters
/// - Audio download and local storage
/// 
/// Implements IAudioGenerator for use with AudioAssetAgent.
/// Does not support speech synthesis (use ElevenLabs for voice).
/// API key is optional (some endpoints may not require auth).
/// Note: Suno API may have rate limits.
/// </summary>
public sealed class SunoAudioGenerator : IAudioGenerator
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly ILogger<SunoAudioGenerator> _logger;

    public SunoAudioGenerator(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<SunoAudioGenerator> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _apiKey = configuration["Suno:ApiKey"]; // Optional - some endpoints may not require auth
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Suno API endpoint (adjust based on actual API)
        _httpClient.BaseAddress = new Uri("https://api.suno.ai/v1/");
        
        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }
    }

    public async Task<GeneratedAudio> GenerateAsync(
        AudioGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Type == AudioType.Voice)
        {
            // Suno doesn't do speech - redirect to ElevenLabs or throw
            throw new NotSupportedException("Suno does not support speech synthesis. Use ElevenLabs for voice generation.");
        }

        var payload = new
        {
            prompt = request.Prompt,
            duration = request.DurationSeconds,
            genre = request.Genre,
            bpm = request.Bpm,
            tags = request.Type == AudioType.Music ? "music" : "sound-effect"
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "generate")
        {
            Content = JsonContent.Create(payload)
        };

        _logger.LogInformation("Generating {Type} audio with Suno: {Prompt}", request.Type, request.Prompt);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SunoGenerationResponse>(cancellationToken: cancellationToken);
        var audioUrl = result?.AudioUrl 
            ?? throw new InvalidOperationException("No audio URL in Suno response");

        // Download audio to local file
        var tempPath = Path.Combine(Path.GetTempPath(), $"suno_{Guid.NewGuid()}.mp3");
        await using var audioStream = await _httpClient.GetStreamAsync(audioUrl, cancellationToken);
        await using var fileStream = File.Create(tempPath);
        await audioStream.CopyToAsync(fileStream, cancellationToken);

        _logger.LogInformation("Generated audio saved to {Path}", tempPath);

        return new GeneratedAudio
        {
            FilePath = tempPath,
            MimeType = "audio/mpeg",
            DurationMs = request.DurationSeconds * 1000,
            SampleRate = 44100,
            Metadata = new Dictionary<string, object>
            {
                ["generator"] = "Suno",
                ["prompt"] = request.Prompt,
                ["type"] = request.Type.ToString(),
                ["genre"] = request.Genre ?? "unknown",
                ["bpm"] = request.Bpm ?? 0
            }
        };
    }

    public Task<GeneratedAudio> GenerateSpeechAsync(
        SpeechGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Suno does not support speech synthesis. Use ElevenLabs for voice generation.");
    }

    private sealed record SunoGenerationResponse(string AudioUrl, string? Status);
}

