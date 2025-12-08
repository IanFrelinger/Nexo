using Nexo.Orchestration.Assets.Ports;

namespace Nexo.Adapters.Assets.Audio;

/// <summary>
/// Echo/placeholder audio generator for testing (doesn't actually generate audio).
/// </summary>
public sealed class EchoAudioGenerator : IAudioGenerator
{
    public Task<GeneratedAudio> GenerateAsync(
        AudioGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        // Create a placeholder audio file
        var tempPath = Path.Combine(Path.GetTempPath(), $"echo_audio_{Guid.NewGuid()}.wav");
        File.WriteAllText(tempPath, "Placeholder audio - replace with real generator");

        return Task.FromResult(new GeneratedAudio
        {
            FilePath = tempPath,
            MimeType = "audio/wav",
            DurationMs = request.DurationSeconds * 1000,
            SampleRate = 44100,
            Metadata = new Dictionary<string, object>
            {
                ["generator"] = "EchoAudioGenerator",
                ["prompt"] = request.Prompt,
                ["type"] = request.Type.ToString(),
                ["genre"] = request.Genre ?? "unknown"
            }
        });
    }

    public Task<GeneratedAudio> GenerateSpeechAsync(
        SpeechGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        // Create a placeholder speech file
        var tempPath = Path.Combine(Path.GetTempPath(), $"echo_speech_{Guid.NewGuid()}.wav");
        File.WriteAllText(tempPath, $"Placeholder speech for: {request.Text}");

        // Estimate duration (rough: 150 words per minute)
        var wordCount = request.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var estimatedDurationMs = (int)(wordCount / 150.0 * 60 * 1000 / request.Speed);

        return Task.FromResult(new GeneratedAudio
        {
            FilePath = tempPath,
            MimeType = "audio/wav",
            DurationMs = estimatedDurationMs,
            SampleRate = 22050,
            Metadata = new Dictionary<string, object>
            {
                ["generator"] = "EchoAudioGenerator",
                ["text"] = request.Text,
                ["voiceId"] = request.VoiceId,
                ["speed"] = request.Speed,
                ["pitch"] = request.Pitch
            }
        });
    }
}

