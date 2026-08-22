using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;
using Ashlar.Orchestration.Agents;
using Ashlar.Abstractions.Agents;
using Ashlar.Orchestration.Architect.Models;
using System.Text.Json;
using ModelInput = Ashlar.Abstractions.ModelInput;

namespace Ashlar.Orchestration.Agents.MultiModal;

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
    /// <summary>Source identifier for the audio (dependency key, URL label, etc.).</summary>
    public required string Source { get; init; }

    /// <summary>Audio payload as base64 or URL.</summary>
    public required string Data { get; init; }

    /// <summary>Audio format (wav, mp3, etc.).</summary>
    public required string Format { get; init; }
}
