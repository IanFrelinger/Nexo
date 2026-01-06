using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Agents.Models;
using Nexo.Orchestration.Architect.Models;
using System.Text.Json;
using ModelInput = Nexo.Abstractions.ModelInput;

namespace Nexo.Orchestration.Agents.MultiModal;

/// <summary>
/// Base agent that supports multiple input modalities (text, image, audio).
/// 
/// Provides functionality for:
/// - Processing multi-modal inputs from dependencies
/// - Extracting text, image, and audio data
/// - Building prompts that include multi-modal content
/// - Format detection and normalization
/// 
/// Derived agents can handle tasks requiring multiple input types simultaneously.
/// Inherits from BaseAgent for lifecycle management.
/// </summary>
public abstract class MultiModalAgentBase : BaseAgent
{
    private readonly IModel? _model;
    private readonly MultiModalProcessor? _processor;

    protected MultiModalAgentBase(
        AgentSpawnSpec spec,
        ILogger<BaseAgent> logger,
        IModel? model = null,
        MultiModalProcessor? processor = null)
        : base(spec, logger)
    {
        _model = model;
        _processor = processor;
    }

    /// <summary>
    /// Processes multi-modal inputs and extracts structured data.
    /// </summary>
    protected async Task<MultiModalInput> ProcessInputAsync(
        IReadOnlyDictionary<string, object>? dependencies,
        CancellationToken cancellationToken)
    {
        var textInputs = new List<string>();
        var imageInputs = new List<ImageInput>();
        var audioInputs = new List<AudioInput>();

        if (dependencies != null)
        {
            foreach (var (key, value) in dependencies)
            {
                if (value is JsonElement jsonElement)
                {
                    // Extract text
                    if (jsonElement.TryGetProperty("text", out var textProp))
                    {
                        textInputs.Add(textProp.GetString() ?? "");
                    }

                    // Extract image
                    if (jsonElement.TryGetProperty("image", out var imageProp))
                    {
                        imageInputs.Add(new ImageInput
                        {
                            Source = key,
                            Data = imageProp.GetString() ?? "",
                            Format = ExtractImageFormat(imageProp.GetString() ?? "")
                        });
                    }

                    // Extract audio
                    if (jsonElement.TryGetProperty("audio", out var audioProp))
                    {
                        audioInputs.Add(new AudioInput
                        {
                            Source = key,
                            Data = audioProp.GetString() ?? "",
                            Format = ExtractAudioFormat(audioProp.GetString() ?? "")
                        });
                    }
                }
                else if (value is string stringValue)
                {
                    // Try to determine if it's an image/audio URL or base64
                    if (stringValue.StartsWith("data:image/"))
                    {
                        imageInputs.Add(new ImageInput
                        {
                            Source = key,
                            Data = stringValue,
                            Format = ExtractImageFormat(stringValue)
                        });
                    }
                    else if (stringValue.StartsWith("data:audio/"))
                    {
                        audioInputs.Add(new AudioInput
                        {
                            Source = key,
                            Data = stringValue,
                            Format = ExtractAudioFormat(stringValue)
                        });
                    }
                    else
                    {
                        textInputs.Add(stringValue);
                    }
                }
            }
        }

        var input = new MultiModalInput
        {
            TextInputs = textInputs,
            ImageInputs = imageInputs,
            AudioInputs = audioInputs
        };

        // Process with multi-modal processor if available
        if (_processor != null)
        {
            return await _processor.ProcessAsync(input, cancellationToken);
        }

        return input;
    }

    private string ExtractImageFormat(string data)
    {
        if (data.StartsWith("data:image/"))
        {
            var parts = data.Split(';');
            if (parts.Length > 0)
            {
                return parts[0].Replace("data:image/", "");
            }
        }
        return "unknown";
    }

    private string ExtractAudioFormat(string data)
    {
        if (data.StartsWith("data:audio/"))
        {
            var parts = data.Split(';');
            if (parts.Length > 0)
            {
                return parts[0].Replace("data:audio/", "");
            }
        }
        return "unknown";
    }

    /// <summary>
    /// Generates a prompt that includes multi-modal content.
    /// </summary>
    protected string BuildMultiModalPrompt(
        string basePrompt,
        MultiModalInput input)
    {
        var prompt = basePrompt;

        if (input.TextInputs.Count > 0)
        {
            prompt += $"\n\nText Inputs:\n{string.Join("\n", input.TextInputs)}";
        }

        if (input.ImageInputs.Count > 0)
        {
            prompt += $"\n\nImage Inputs ({input.ImageInputs.Count}):";
            foreach (var img in input.ImageInputs)
            {
                prompt += $"\n- {img.Source} ({img.Format})";
            }
        }

        if (input.AudioInputs.Count > 0)
        {
            prompt += $"\n\nAudio Inputs ({input.AudioInputs.Count}):";
            foreach (var audio in input.AudioInputs)
            {
                prompt += $"\n- {audio.Source} ({audio.Format})";
            }
        }

        return prompt;
    }
}

/// <summary>
/// Multi-modal input containing text, images, and audio.
/// 
/// Contains:
/// - List of text inputs
/// - List of image inputs with source, data, and format
/// - List of audio inputs with source, data, and format
/// 
/// Used by MultiModalAgentBase to process multi-modal content.
/// </summary>
public sealed record MultiModalInput
{
    public IReadOnlyList<string> TextInputs { get; init; } = new List<string>();
    public IReadOnlyList<ImageInput> ImageInputs { get; init; } = new List<ImageInput>();
    public IReadOnlyList<AudioInput> AudioInputs { get; init; } = new List<AudioInput>();
}

/// <summary>
/// Image input data.
/// 
/// Contains:
/// - Source identifier (e.g., dependency key)
/// - Image data (base64 or URL)
/// - Image format (e.g., "png", "jpg")
/// 
/// Used by MultiModalAgentBase to represent image inputs.
/// </summary>
public sealed record ImageInput
{
    public required string Source { get; init; }
    public required string Data { get; init; }
    public required string Format { get; init; }
}

/// <summary>
/// Audio input data.
/// 
/// Contains:
/// - Source identifier (e.g., dependency key)
/// - Audio data (base64 or URL)
/// - Audio format (e.g., "wav", "mp3")
/// 
/// Used by MultiModalAgentBase to represent audio inputs.
/// </summary>
public sealed record AudioInput
{
    public required string Source { get; init; }
    public required string Data { get; init; }
    public required string Format { get; init; }
}

/// <summary>
/// Processor for multi-modal inputs.
/// 
/// Responsibilities:
/// - Extracts features from images (OCR, object detection)
/// - Transcribes audio to text
/// - Normalizes and validates inputs
/// 
/// Used by MultiModalAgentBase to process multi-modal content.
/// Currently a placeholder - future implementation would perform actual processing.
/// </summary>
public sealed class MultiModalProcessor
{
    private readonly ILogger<MultiModalProcessor>? _logger;

    public MultiModalProcessor(ILogger<MultiModalProcessor>? logger = null)
    {
        _logger = logger;
    }

    public Task<MultiModalInput> ProcessAsync(
        MultiModalInput input,
        CancellationToken cancellationToken)
    {
        // In a real implementation, this would:
        // - Extract features from images (OCR, object detection)
        // - Transcribe audio to text
        // - Normalize and validate inputs

        _logger?.LogDebug("Processed multi-modal input: {TextCount} text, {ImageCount} images, {AudioCount} audio",
            input.TextInputs.Count, input.ImageInputs.Count, input.AudioInputs.Count);

        return Task.FromResult(input);
    }
}

