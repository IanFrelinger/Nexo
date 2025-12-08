using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Assets.Ports;

namespace Nexo.Adapters.Assets.Audio;

/// <summary>
/// ElevenLabs speech synthesis implementation.
/// </summary>
public sealed class ElevenLabsAudioGenerator : IAudioGenerator
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<ElevenLabsAudioGenerator> _logger;

    public ElevenLabsAudioGenerator(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ElevenLabsAudioGenerator> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _apiKey = configuration["ElevenLabs:ApiKey"]
            ?? throw new InvalidOperationException("ElevenLabs API key not configured");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _httpClient.BaseAddress = new Uri("https://api.elevenlabs.io/v1/");
        _httpClient.DefaultRequestHeaders.Add("xi-api-key", _apiKey);
    }

    public Task<GeneratedAudio> GenerateAsync(
        AudioGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        // ElevenLabs primarily does speech synthesis
        // For music/sound effects, we'd need a different service
        _logger.LogWarning("ElevenLabs is optimized for speech. Use GenerateSpeechAsync for best results.");
        
        // Fallback: treat as speech with prompt as text
        return GenerateSpeechAsync(new SpeechGenerationRequest
        {
            Text = request.Prompt,
            VoiceId = "default",
            Speed = 1.0,
            Pitch = 1.0
        }, cancellationToken);
    }

    public async Task<GeneratedAudio> GenerateSpeechAsync(
        SpeechGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        var voiceId = request.VoiceId;
        if (voiceId == "default")
        {
            // Get default voice
            voiceId = await GetDefaultVoiceIdAsync(cancellationToken);
        }

        var payload = new
        {
            text = request.Text,
            model_id = "eleven_multilingual_v2",
            voice_settings = new
            {
                stability = 0.5,
                similarity_boost = 0.75,
                style = 0.0,
                use_speaker_boost = true
            }
        };

        var url = $"text-to-speech/{voiceId}";
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload)
        };

        _logger.LogInformation("Generating speech with ElevenLabs: {TextLength} characters, voice {VoiceId}", 
            request.Text.Length, voiceId);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        // Download audio to local file
        var tempPath = Path.Combine(Path.GetTempPath(), $"elevenlabs_{Guid.NewGuid()}.mp3");
        await using var audioStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = File.Create(tempPath);
        await audioStream.CopyToAsync(fileStream, cancellationToken);

        // Estimate duration (rough: 150 words per minute)
        var wordCount = request.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var estimatedDurationMs = (int)(wordCount / 150.0 * 60 * 1000 / request.Speed);

        _logger.LogInformation("Generated speech saved to {Path}", tempPath);

        return new GeneratedAudio
        {
            FilePath = tempPath,
            MimeType = "audio/mpeg",
            DurationMs = estimatedDurationMs,
            SampleRate = 44100,
            Metadata = new Dictionary<string, object>
            {
                ["generator"] = "ElevenLabs",
                ["voiceId"] = voiceId,
                ["text"] = request.Text,
                ["speed"] = request.Speed,
                ["pitch"] = request.Pitch
            }
        };
    }

    private async Task<string> GetDefaultVoiceIdAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync("voices", cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ElevenLabsVoicesResponse>(cancellationToken: cancellationToken);
            return result?.Voices?.FirstOrDefault()?.VoiceId ?? "21m00Tcm4TlvDq8ikWAM"; // Default fallback
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch voices, using default");
            return "21m00Tcm4TlvDq8ikWAM"; // Default voice ID
        }
    }

    private sealed record ElevenLabsVoicesResponse(ElevenLabsVoice[] Voices);
    private sealed record ElevenLabsVoice(string VoiceId, string Name);
}

