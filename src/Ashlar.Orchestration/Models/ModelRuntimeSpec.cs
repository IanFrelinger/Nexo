using System.Text.Json;
using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;

namespace Ashlar.Orchestration.Models;

/// <summary>Model provider and behavior preferences for a single runtime scope.</summary>
public sealed record ModelRuntimeSpec
{
    /// <summary>Execution mode preference: agentic, deterministic, or auto.</summary>
    public string Prefer { get; init; } = "auto";

    /// <summary>Provider name (openai, azure, ollama, offline, mock-json, etc.).</summary>
    public string? Provider { get; init; }

    /// <summary>Optional model identifier (for example an Ollama model or tag name).</summary>
    public string? Model { get; init; }
}
