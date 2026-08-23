using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;
using Ashlar.Orchestration.Agents;
using Ashlar.Abstractions.Agents;
using Ashlar.Orchestration.Architect.Models;
using System.Text.Json;
using ModelInput = Ashlar.Abstractions.ModelInput;

namespace Ashlar.Orchestration.Agents.MultiModal;

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
    /// <summary>Source identifier for the image (dependency key, URL label, etc.).</summary>
    public required string Source { get; init; }

    /// <summary>Image payload as base64 or URL.</summary>
    public required string Data { get; init; }

    /// <summary>Image format (png, jpg, etc.).</summary>
    public required string Format { get; init; }
}
