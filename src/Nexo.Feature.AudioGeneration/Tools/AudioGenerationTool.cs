using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;
using Nexo.Agent.Models;
using Nexo.Feature.AudioGeneration.Interfaces;
using Nexo.Feature.AudioGeneration.Models;
using Nexo.Feature.AudioGeneration.Services;

namespace Nexo.Feature.AudioGeneration.Tools;

/// <summary>
/// Agent tool for audio generation
/// </summary>
public partial class AudioGenerationTool : ITool
{
    private readonly ILogger<AudioGenerationTool> _logger;
    private readonly AudioGenerationOrchestrator _orchestrator;

    public AudioGenerationTool(
        ILogger<AudioGenerationTool> logger,
        AudioGenerationOrchestrator orchestrator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
    }

    public ToolManifest Manifest => new(
        Id: "tool.audio.generation",
        Name: "Audio Generation",
        Version: "1.0.0",
        Description: "Generates audio from text prompts using AI models",
        Author: "Nexo Audio Generation",
        InputSchema: """
        {
            "type": "object",
            "properties": {
                "prompt": {"type": "string", "description": "Text prompt describing the audio to generate"},
                "audioType": {"type": "string", "enum": ["SoundEffect", "Music", "Voice", "Ambient", "UI", "Environmental"], "default": "SoundEffect"},
                "duration": {"type": "number", "default": 5.0, "description": "Duration in seconds"},
                "sampleRate": {"type": "integer", "default": 44100, "description": "Sample rate in Hz"},
                "channels": {"type": "integer", "default": 2, "description": "Number of channels (1=mono, 2=stereo)"},
                "style": {"type": "string", "description": "Audio style or genre"},
                "volume": {"type": "number", "default": 0.8, "description": "Volume level (0.0 to 1.0)"},
                "providerId": {"type": "string", "description": "Specific provider to use (optional)"}
            },
            "required": ["prompt"]
        }
        """,
        OutputSchema: """
        {
            "type": "object",
            "properties": {
                "success": {"type": "boolean"},
                "audioData": {"type": "string", "description": "Generated audio as base64 string"},
                "filePath": {"type": "string", "description": "File path where audio was saved"},
                "format": {"type": "string", "description": "Audio format (WAV, MP3, etc.)"},
                "duration": {"type": "number", "description": "Duration of generated audio"},
                "sampleRate": {"type": "integer", "description": "Sample rate of generated audio"},
                "channels": {"type": "integer", "description": "Number of channels"},
                "error": {"type": "string", "description": "Error message if generation failed"},
                "generationTimeMs": {"type": "integer", "description": "Generation time in milliseconds"}
            }
        }
        """,
        RequiredPermissions: ToolPermissions.FileWrite,
        Capabilities: new[] { "audio-generation", "ai", "offline", "online" },
        Timeout: TimeSpan.FromMinutes(5),
        CreatedAt: DateTime.UtcNow
    );

    public async Task<string> ExecuteAsync(string input, ToolContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Executing audio generation tool with input: {Input}", input);

            var requestData = JsonSerializer.Deserialize<JsonElement>(input);
            
            var request = new AudioGenerationRequest
            {
                Prompt = requestData.GetProperty("prompt").GetString() ?? string.Empty,
                AudioType = Enum.TryParse<AudioType>(requestData.GetProperty("audioType").GetString(), out var audioType) 
                    ? audioType : AudioType.SoundEffect,
                Duration = requestData.TryGetProperty("duration", out var durationProp) 
                    ? durationProp.GetSingle() : 5.0f,
                SampleRate = requestData.TryGetProperty("sampleRate", out var sampleRateProp) 
                    ? sampleRateProp.GetInt32() : 44100,
                Channels = requestData.TryGetProperty("channels", out var channelsProp) 
                    ? channelsProp.GetInt32() : 2,
                Style = requestData.TryGetProperty("style", out var styleProp) 
                    ? styleProp.GetString() : null,
                Volume = requestData.TryGetProperty("volume", out var volumeProp) 
                    ? volumeProp.GetSingle() : 0.8f
            };

            var providerId = requestData.TryGetProperty("providerId", out var providerProp) 
                ? providerProp.GetString() : null;

            var result = await _orchestrator.GenerateAudioAsync(request, providerId, cancellationToken);

            var response = new
            {
                success = result.Success,
                audioData = result.AudioData != null ? Convert.ToBase64String(result.AudioData) : null,
                filePath = result.FilePath,
                format = result.Format,
                duration = result.Duration,
                sampleRate = result.SampleRate,
                channels = result.Channels,
                error = result.Error,
                generationTimeMs = result.GenerationTimeMs
            };

            return JsonSerializer.Serialize(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing audio generation tool");
            
            var errorResponse = new
            {
                success = false,
                error = ex.Message,
                generationTimeMs = 0
            };

            return JsonSerializer.Serialize(errorResponse);
        }
    }
}
