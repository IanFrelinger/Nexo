using Nexo.Core.Domain.Bricks;

namespace Nexo.Agents.UniversalTester.Configuration;

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
    /// Number of recent screenshots to send for multi-frame vision (Ollama). 0 disables multi-frame; 1 uses single-frame only.
    /// Default 4 gives temporal context without excessive tokens.
    /// </summary>
    public int MultiFrameCount { get; init; } = 4;

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
