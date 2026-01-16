using System.Text.Json;
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

    public static UniversalTesterRuntimeConfig Default()
    {
        return new UniversalTesterRuntimeConfig
        {
            Prefer = "auto",
            Bricks = new Dictionary<string, BrickRuntimeSpec>(StringComparer.OrdinalIgnoreCase)
            {
                ["perception"] = BrickRuntimeSpec.DeterministicOnly(),
                ["action"] = BrickRuntimeSpec.DeterministicOnly(),

                // Prefer agentic, but always allow deterministic fallback.
                ["understanding"] = BrickRuntimeSpec.AgenticWithDeterministicFallback(),
                ["exploration"] = BrickRuntimeSpec.AgenticWithDeterministicFallback(),
                ["validation"] = BrickRuntimeSpec.AgenticWithDeterministicFallback(),
                ["reporting"] = BrickRuntimeSpec.AgenticWithDeterministicFallback()
            }
        };
    }
}

public static class UniversalTesterRuntimeConfigLoader
{
    public static UniversalTesterRuntimeConfig Load(string? filePath, string? json)
    {
        if (!string.IsNullOrWhiteSpace(json))
        {
            return Deserialize(json);
        }

        if (!string.IsNullOrWhiteSpace(filePath))
        {
            var text = File.ReadAllText(filePath);
            return Deserialize(text);
        }

        return UniversalTesterRuntimeConfig.Default();
    }

    private static UniversalTesterRuntimeConfig Deserialize(string json)
    {
        var cfg = JsonSerializer.Deserialize<UniversalTesterRuntimeConfig>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

        return cfg ?? throw new InvalidOperationException("Failed to parse universal tester runtime config JSON");
    }
}

