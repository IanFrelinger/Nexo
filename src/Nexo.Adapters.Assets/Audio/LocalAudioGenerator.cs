using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Assets.Ports;

namespace Nexo.Adapters.Assets.Audio;

/// <summary>
/// Local/self-hosted audio generation adapter.
/// Supports Bark local, Piper TTS, Coqui TTS, and custom endpoints.
/// </summary>
public sealed class LocalAudioGenerator : IAudioGenerator
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly LocalAudioProvider _provider;
    private readonly ILogger<LocalAudioGenerator> _logger;

    public LocalAudioGenerator(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<LocalAudioGenerator> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _baseUrl = configuration["LocalAudioGenerator:BaseUrl"]
            ?? throw new InvalidOperationException("LocalAudioGenerator:BaseUrl not configured");
        _provider = Enum.Parse<LocalAudioProvider>(
            configuration["LocalAudioGenerator:Provider"] ?? "Bark");

        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(10); // Audio generation can take time

        var apiKey = configuration["LocalAudioGenerator:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }
    }

    public async Task<GeneratedAudio> GenerateAsync(
        AudioGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        return _provider switch
        {
            LocalAudioProvider.Bark => await GenerateBarkAsync(request, cancellationToken),
            LocalAudioProvider.Custom => await GenerateCustomAsync(request, cancellationToken),
            _ => throw new NotSupportedException($"Provider {_provider} does not support audio generation. Use GenerateSpeechAsync for TTS.")
        };
    }

    public async Task<GeneratedAudio> GenerateSpeechAsync(
        SpeechGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        return _provider switch
        {
            LocalAudioProvider.Bark => await GenerateBarkSpeechAsync(request, cancellationToken),
            LocalAudioProvider.Piper => await GeneratePiperAsync(request, cancellationToken),
            LocalAudioProvider.Coqui => await GenerateCoquiAsync(request, cancellationToken),
            LocalAudioProvider.Custom => await GenerateCustomSpeechAsync(request, cancellationToken),
            _ => throw new NotSupportedException($"Provider {_provider} not supported for speech")
        };
    }

    private async Task<GeneratedAudio> GenerateBarkAsync(
        AudioGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            text = request.Prompt,
            duration = request.DurationSeconds,
            type = request.Type.ToString().ToLowerInvariant(),
            genre = request.Genre,
            bpm = request.Bpm
        };

        _logger.LogInformation("Generating {Type} audio with Bark: {Prompt}", request.Type, request.Prompt);

        var response = await _httpClient.PostAsJsonAsync("generate", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<BarkResponse>(cancellationToken: cancellationToken);
        var audioUrl = result?.AudioUrl ?? result?.AudioPath
            ?? throw new InvalidOperationException("No audio URL/path in Bark response");

        var audioPath = await DownloadOrUsePathAsync(audioUrl, cancellationToken);

        return new GeneratedAudio
        {
            FilePath = audioPath,
            MimeType = "audio/wav",
            DurationMs = request.DurationSeconds * 1000,
            SampleRate = 24000,
            Metadata = new Dictionary<string, object>
            {
                ["generator"] = "Bark",
                ["provider"] = "local",
                ["type"] = request.Type.ToString()
            }
        };
    }

    private async Task<GeneratedAudio> GenerateBarkSpeechAsync(
        SpeechGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            text = request.Text,
            voice = request.VoiceId,
            speed = request.Speed,
            pitch = request.Pitch
        };

        _logger.LogInformation("Generating speech with Bark: {TextLength} characters", request.Text.Length);

        var response = await _httpClient.PostAsJsonAsync("tts", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var audioBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var tempPath = Path.Combine(Path.GetTempPath(), $"bark_speech_{Guid.NewGuid()}.wav");
        await File.WriteAllBytesAsync(tempPath, audioBytes, cancellationToken);

        var wordCount = request.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var estimatedDurationMs = (int)(wordCount / 150.0 * 60 * 1000 / request.Speed);

        return new GeneratedAudio
        {
            FilePath = tempPath,
            MimeType = "audio/wav",
            DurationMs = estimatedDurationMs,
            SampleRate = 24000,
            Metadata = new Dictionary<string, object>
            {
                ["generator"] = "Bark",
                ["provider"] = "local",
                ["voiceId"] = request.VoiceId
            }
        };
    }

    private async Task<GeneratedAudio> GeneratePiperAsync(
        SpeechGenerationRequest request,
        CancellationToken cancellationToken)
    {
        // Piper TTS API format
        var payload = new
        {
            text = request.Text,
            model = request.VoiceId,
            speaker_id = 0,
            length_penalty = 1.0,
            speed = request.Speed
        };

        _logger.LogInformation("Generating speech with Piper: {TextLength} characters", request.Text.Length);

        var response = await _httpClient.PostAsJsonAsync("api/tts", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var audioBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var tempPath = Path.Combine(Path.GetTempPath(), $"piper_{Guid.NewGuid()}.wav");
        await File.WriteAllBytesAsync(tempPath, audioBytes, cancellationToken);

        var wordCount = request.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var estimatedDurationMs = (int)(wordCount / 150.0 * 60 * 1000 / request.Speed);

        return new GeneratedAudio
        {
            FilePath = tempPath,
            MimeType = "audio/wav",
            DurationMs = estimatedDurationMs,
            SampleRate = 22050, // Piper default
            Metadata = new Dictionary<string, object>
            {
                ["generator"] = "Piper",
                ["provider"] = "local"
            }
        };
    }

    private async Task<GeneratedAudio> GenerateCoquiAsync(
        SpeechGenerationRequest request,
        CancellationToken cancellationToken)
    {
        // Coqui TTS API format
        var payload = new
        {
            text = request.Text,
            speaker_id = request.VoiceId,
            language_id = "en",
            speed = request.Speed
        };

        _logger.LogInformation("Generating speech with Coqui: {TextLength} characters", request.Text.Length);

        var response = await _httpClient.PostAsJsonAsync("api/tts", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var audioBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var tempPath = Path.Combine(Path.GetTempPath(), $"coqui_{Guid.NewGuid()}.wav");
        await File.WriteAllBytesAsync(tempPath, audioBytes, cancellationToken);

        var wordCount = request.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var estimatedDurationMs = (int)(wordCount / 150.0 * 60 * 1000 / request.Speed);

        return new GeneratedAudio
        {
            FilePath = tempPath,
            MimeType = "audio/wav",
            DurationMs = estimatedDurationMs,
            SampleRate = 22050,
            Metadata = new Dictionary<string, object>
            {
                ["generator"] = "Coqui",
                ["provider"] = "local"
            }
        };
    }

    private async Task<GeneratedAudio> GenerateCustomAsync(
        AudioGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            prompt = request.Prompt,
            duration = request.DurationSeconds,
            type = request.Type.ToString(),
            genre = request.Genre,
            bpm = request.Bpm
        };

        _logger.LogInformation("Generating audio with custom endpoint: {Prompt}", request.Prompt);

        var response = await _httpClient.PostAsJsonAsync("generate", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        
        string audioPath;
        if (result.TryGetProperty("audio_url", out var urlElement))
        {
            audioPath = await DownloadOrUsePathAsync(urlElement.GetString()!, cancellationToken);
        }
        else if (result.TryGetProperty("audio_base64", out var b64Element))
        {
            var audioBytes = Convert.FromBase64String(b64Element.GetString()!);
            audioPath = Path.Combine(Path.GetTempPath(), $"custom_audio_{Guid.NewGuid()}.wav");
            await File.WriteAllBytesAsync(audioPath, audioBytes, cancellationToken);
        }
        else
        {
            // Try direct binary response
            var audioBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            audioPath = Path.Combine(Path.GetTempPath(), $"custom_audio_{Guid.NewGuid()}.wav");
            await File.WriteAllBytesAsync(audioPath, audioBytes, cancellationToken);
        }

        return new GeneratedAudio
        {
            FilePath = audioPath,
            MimeType = "audio/wav",
            DurationMs = request.DurationSeconds * 1000,
            SampleRate = 22050,
            Metadata = new Dictionary<string, object>
            {
                ["generator"] = "Custom",
                ["provider"] = "local"
            }
        };
    }

    private async Task<GeneratedAudio> GenerateCustomSpeechAsync(
        SpeechGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            text = request.Text,
            voice_id = request.VoiceId,
            speed = request.Speed,
            pitch = request.Pitch
        };

        _logger.LogInformation("Generating speech with custom endpoint: {TextLength} characters", request.Text.Length);

        var response = await _httpClient.PostAsJsonAsync("tts", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var audioBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var tempPath = Path.Combine(Path.GetTempPath(), $"custom_speech_{Guid.NewGuid()}.wav");
        await File.WriteAllBytesAsync(tempPath, audioBytes, cancellationToken);

        var wordCount = request.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var estimatedDurationMs = (int)(wordCount / 150.0 * 60 * 1000 / request.Speed);

        return new GeneratedAudio
        {
            FilePath = tempPath,
            MimeType = "audio/wav",
            DurationMs = estimatedDurationMs,
            SampleRate = 22050,
            Metadata = new Dictionary<string, object>
            {
                ["generator"] = "Custom",
                ["provider"] = "local"
            }
        };
    }

    private async Task<string> DownloadOrUsePathAsync(string pathOrUrl, CancellationToken cancellationToken)
    {
        if (File.Exists(pathOrUrl))
        {
            return pathOrUrl; // Local file path
        }

        if (Uri.TryCreate(pathOrUrl, UriKind.Absolute, out var uri))
        {
            // Download from URL
            var response = await _httpClient.GetAsync(pathOrUrl, cancellationToken);
            response.EnsureSuccessStatusCode();
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var tempPath = Path.Combine(Path.GetTempPath(), $"downloaded_audio_{Guid.NewGuid()}.wav");
            await File.WriteAllBytesAsync(tempPath, bytes, cancellationToken);
            return tempPath;
        }

        throw new InvalidOperationException($"Invalid audio path or URL: {pathOrUrl}");
    }

    private sealed record BarkResponse(string? AudioUrl, string? AudioPath);
}

/// <summary>
/// Supported local audio generation providers.
/// </summary>
public enum LocalAudioProvider
{
    /// <summary>
    /// Bark (local instance).
    /// </summary>
    Bark,

    /// <summary>
    /// Piper TTS (text-to-speech only).
    /// </summary>
    Piper,

    /// <summary>
    /// Coqui TTS (text-to-speech only).
    /// </summary>
    Coqui,

    /// <summary>
    /// Custom endpoint.
    /// </summary>
    Custom
}

