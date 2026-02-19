using Nexo.Core.Domain.Bricks;

namespace Nexo.Agents.UniversalTester.Configuration;

/// <summary>
/// Understanding analysis mode. Affects how the Understanding brick interprets perception.
/// </summary>
public enum UnderstandingMode
{
    /// <summary>Infer from context: GameState present → gameStateFirst; multi-frame → temporal; else pixelOnly.</summary>
    Auto,

    /// <summary>Vision + DOM only; no structured game state.</summary>
    PixelOnly,

    /// <summary>When GameState is available, prioritize it; vision supplements.</summary>
    GameStateFirst,

    /// <summary>Emphasize temporal flow; use gameplay-specific prompts for multi-frame.</summary>
    Temporal
}

/// <summary>
/// Per-adapter configuration. Keys: host, port, type (for future custom adapters).
/// </summary>
public sealed record AdapterConfig
{
    /// <summary>Override host for adapter connection (e.g. game plugin TCP host).</summary>
    public string? Host { get; init; }
    /// <summary>Override port for adapter connection (e.g. game plugin TCP port).</summary>
    public int? Port { get; init; }
    /// <summary>Adapter type for future custom adapters.</summary>
    public string? Type { get; init; }
}

/// <summary>
/// Per-brick overrides for model, mode, provider, prompt template. Extends BrickRuntimeSpec.
/// </summary>
public sealed record BrickOverrideConfig
{
    /// <summary>Understanding mode: pixelOnly, gameStateFirst, temporal.</summary>
    public string? Mode { get; init; }
    /// <summary>Model override (e.g. llava:7b for Ollama).</summary>
    public string? Model { get; init; }
    /// <summary>Provider override (e.g. ollama, video).</summary>
    public string? Provider { get; init; }
    /// <summary>Named template key for prompt template registry (e.g. "temporal-gameplay").</summary>
    public string? PromptTemplate { get; init; }
    /// <summary>Additional key-value config for the brick.</summary>
    public IReadOnlyDictionary<string, object>? CustomConfig { get; init; }
}


/// <summary>
/// Runtime configuration for how the Universal Tester selects between agentic vs deterministic brick implementations.
/// Intended to be loaded from JSON at runtime (CLI/demo), and supports per-brick preferences + fallback.
/// </summary>
public sealed record UniversalTesterRuntimeConfig
{
    /// <summary>
    /// Global preference: "agentic", "deterministic", or "auto".
    /// "auto" means: prefer the brick's default, with fallback if unavailable/fails.
    /// </summary>
    public string Prefer { get; init; } = "auto";

    /// <summary>
    /// Per-brick configuration keyed by logical brick name: perception, understanding, exploration, action, validation, reporting.
    /// </summary>
    public Dictionary<string, BrickRuntimeSpec> Bricks { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Per-brick overrides for understanding mode, model, provider. Keys: perception, understanding, exploration, etc.
    /// </summary>
    public Dictionary<string, BrickOverrideConfig> BrickOverrides { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Understanding mode: auto, pixelOnly, gameStateFirst, temporal. Affects prompts and state handling.
    /// Overridable per-brick via BrickOverrides["understanding"].Mode.
    /// </summary>
    public UnderstandingMode UnderstandingMode { get; init; } = UnderstandingMode.Auto;

    /// <summary>
    /// Ordered brick IDs for the test pipeline. If null/empty, uses default fixed pipeline.
    /// Enables optional bricks (e.g. world-model-prediction) in future.
    /// </summary>
    public IReadOnlyList<string>? Pipeline { get; init; }

    /// <summary>
    /// Per-adapter overrides. Keys: game, webapp, desktop, api, cli (match TargetType).
    /// Example: "game": { "host": "192.168.1.5", "port": 9999 }
    /// </summary>
    public Dictionary<string, AdapterConfig> Adapters { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Number of recent screenshots to send for multi-frame vision (Ollama). 0 disables multi-frame; 1 uses single-frame only.
    /// Default 4 gives temporal context without excessive tokens.
    /// </summary>
    public int MultiFrameCount { get; init; } = 4;

    /// <summary>
    /// Optional path to a JSON file with additional prompt templates. Merged with built-in templates.
    /// File format: { "templateName": { "systemPrompt": "...", "userPromptTemplate": "..." }, ... }
    /// </summary>
    public string? PromptTemplatesPath { get; init; }

    /// <summary>Returns default runtime config with standard brick preferences and pipeline.</summary>
    public static UniversalTesterRuntimeConfig Default()
    {
        return new UniversalTesterRuntimeConfig
        {
            Prefer = "auto",
            Bricks = new Dictionary<string, BrickRuntimeSpec>(StringComparer.OrdinalIgnoreCase)
            {
                ["perception"] = BrickRuntimeSpec.AgenticWithDeterministicFallback(),
                ["action"] = BrickRuntimeSpec.DeterministicOnly(),

                // Agentic only; no fallback. Vision/LLM must be reachable.
                ["understanding"] = BrickRuntimeSpec.AgenticOnly(),
                ["exploration"] = BrickRuntimeSpec.AgenticOnly(),
                ["validation"] = BrickRuntimeSpec.AgenticWithDeterministicFallback(),
                ["reporting"] = BrickRuntimeSpec.AgenticWithDeterministicFallback()
            }
        };
    }
}
