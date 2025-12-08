using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Assets.Ports;

namespace Nexo.Adapters.Assets.Audio;

/// <summary>
/// Bark AI audio generation implementation (supports music, sound effects, and speech).
/// </summary>
public sealed class BarkAudioGenerator : IAudioGenerator
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly ILogger<BarkAudioGenerator> _logger;

    public BarkAudioGenerator(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<BarkAudioGenerator> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _apiKey = configuration["Bark:ApiKey"]; // Optional - Bark may have open endpoints
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Bark API endpoint (adjust based on actual API)
        _httpClient.BaseAddress = new Uri("https://api.bark.ai/v1/");
        
        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }
    }

    public async Task<GeneratedAudio> GenerateAsync(
        AudioGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            text = request.Prompt,
            duration = request.DurationSeconds,
            type = request.Type.ToString().ToLowerInvariant(),
            genre = request.Genre,
            bpm = request.Bpm
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "generate")
        {
            Content = JsonContent.Create(payload)
        };

        _logger.LogInformation("Generating {Type} audio with Bark: {Prompt}", request.Type, request.Prompt);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<BarkGenerationResponse>(cancellationToken: cancellationToken);
        var audioUrl = result?.AudioUrl
            ?? throw new InvalidOperationException("No audio URL in Bark response");

        // Download audio to local file
        var extension = request.Type == AudioType.Music ? ".mp3" : ".wav";
        var tempPath = Path.Combine(Path.GetTempPath(), $"bark_{Guid.NewGuid()}{extension}");
        await using var audioStream = await _httpClient.GetStreamAsync(audioUrl, cancellationToken);
        await using var fileStream = File.Create(tempPath);
        await audioStream.CopyToAsync(fileStream, cancellationToken);

        _logger.LogInformation("Generated audio saved to {Path}", tempPath);

        return new GeneratedAudio
        {
            FilePath = tempPath,
            MimeType = extension == ".mp3" ? "audio/mpeg" : "audio/wav",
            DurationMs = request.DurationSeconds * 1000,
            SampleRate = 24000, // Bark's default sample rate
            Metadata = new Dictionary<string, object>
            {
                ["generator"] = "Bark",
                ["prompt"] = request.Prompt,
                ["type"] = request.Type.ToString(),
                ["genre"] = request.Genre ?? "unknown",
                ["bpm"] = request.Bpm ?? 0
            }
        };
    }

    public async Task<GeneratedAudio> GenerateSpeechAsync(
        SpeechGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        // Bark can generate speech from text
        var payload = new
        {
            text = request.Text,
            voice = request.VoiceId,
            speed = request.Speed,
            pitch = request.Pitch
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "speech")
        {
            Content = JsonContent.Create(payload)
        };

        _logger.LogInformation("Generating speech with Bark: {TextLength} characters", request.Text.Length);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<BarkGenerationResponse>(cancellationToken: cancellationToken);
        var audioUrl = result?.AudioUrl
            ?? throw new InvalidOperationException("No audio URL in Bark response");

        // Download audio to local file
        var tempPath = Path.Combine(Path.GetTempPath(), $"bark_speech_{Guid.NewGuid()}.wav");
        await using var audioStream = await _httpClient.GetStreamAsync(audioUrl, cancellationToken);
        await using var fileStream = File.Create(tempPath);
        await audioStream.CopyToAsync(fileStream, cancellationToken);

        // Estimate duration
        var wordCount = request.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var estimatedDurationMs = (int)(wordCount / 150.0 * 60 * 1000 / request.Speed);

        _logger.LogInformation("Generated speech saved to {Path}", tempPath);

        return new GeneratedAudio
        {
            FilePath = tempPath,
            MimeType = "audio/wav",
            DurationMs = estimatedDurationMs,
            SampleRate = 24000,
            Metadata = new Dictionary<string, object>
            {
                ["generator"] = "Bark",
                ["text"] = request.Text,
                ["voiceId"] = request.VoiceId,
                ["speed"] = request.Speed,
                ["pitch"] = request.Pitch
            }
        };
    }

    private sealed record BarkGenerationResponse(string AudioUrl, string? Status);
}

