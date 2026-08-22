using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;
using Ashlar.Orchestration.Agents;
using Ashlar.Abstractions.Agents;
using Ashlar.Orchestration.Architect.Models;
using System.Text.Json;
using ModelInput = Ashlar.Abstractions.ModelInput;

namespace Ashlar.Orchestration.Agents.MultiModal;

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
