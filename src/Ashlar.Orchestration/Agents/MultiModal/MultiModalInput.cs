using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;
using Ashlar.Orchestration.Agents;
using Ashlar.Abstractions.Agents;
using Ashlar.Orchestration.Architect.Models;
using System.Text.Json;
using ModelInput = Ashlar.Abstractions.ModelInput;

namespace Ashlar.Orchestration.Agents.MultiModal;

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
    /// <summary>Text inputs supplied to the agent.</summary>
    public IReadOnlyList<string> TextInputs { get; init; } = new List<string>();

    /// <summary>Image inputs supplied to the agent.</summary>
    public IReadOnlyList<ImageInput> ImageInputs { get; init; } = new List<ImageInput>();

    /// <summary>Audio inputs supplied to the agent.</summary>
    public IReadOnlyList<AudioInput> AudioInputs { get; init; } = new List<AudioInput>();
}
