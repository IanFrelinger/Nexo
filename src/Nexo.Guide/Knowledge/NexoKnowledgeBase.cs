using System.Text.RegularExpressions;

namespace Nexo.Guide.Knowledge;

public class NexoKnowledgeBase
{
    private readonly Dictionary<string, KnowledgeSection> _sections = new()
    {
        ["core"] = new("Core Description",
            "Nexo is an AI-native runtime framework — 115K+ lines of C#. " +
            "It enables software to think, act, and learn autonomously. " +
            "Three-layer composition: Agents → Behaviors → Bricks. " +
            "Dual implementation: Deterministic (⚙️) for rules, Agentic (🤖) for reasoning. " +
            "Runs fully offline. Air-gap compatible. No vendor lock-in.",
            new[] { "nexo", "what", "explain", "describe", "about", "overview" }),

        ["architecture"] = new("Architecture",
            "Three layers: Agents (IAgent) — autonomous with state/decisions. " +
            "Behaviors (IBehavior) — DAGs composing bricks into workflows. " +
            "Bricks (IBrick) — atomic capabilities, typed Input→Output. " +
            "DeterministicBrick or AgenticBrick — swap at runtime. " +
            "BrickExecutionEngine runs all. CompositeBrickRegistry aggregates local+remote. " +
            "Hexagonal architecture. Clean Architecture. CQRS with MediatR.",
            new[] { "architecture", "layer", "structure", "brick", "behavior", "agent", "design" }),

        ["chatgpt_comparison"] = new("ChatGPT Comparison",
            "ChatGPT: brain in jar — thinks/talks but no action, no persistence, cloud-only. " +
            "Nexo: brain with body — monitors, acts, remembers permanently, runs locally. " +
            "ChatGPT: per-API-call cost. Nexo: zero marginal cost. " +
            "ChatGPT: data to OpenAI. Nexo: data never leaves device. " +
            "ChatGPT: resets every conversation. Nexo: accumulates capabilities.",
            new[] { "chatgpt", "gpt", "openai", "different", "compare", "vs" }),

        ["offline"] = new("Offline Capability",
            "Entire stack local: Nexo runtime + Ollama + your code. " +
            "No external API calls. IOllamaClient hits localhost:11434. " +
            "Self-contained .NET binary. Copy to USB. Run in SCIF. " +
            "NIST 800-171, CMMC compatible. IL4/5/6 environments.",
            new[] { "offline", "local", "air.?gap", "scif", "classified", "privacy" }),

        ["self_extension"] = new("Self-Extension",
            "Nexo solves a problem → creates a new brick → persists to registry. " +
            "System builds ever-growing capability toolkit. " +
            "Multiple instances share bricks via neuroplastic networking. " +
            "One learns → all learn. Automatically.",
            new[] { "learn", "self.?extend", "grow", "smarter", "create.*brick", "evolve" }),

        ["networking"] = new("Neuroplastic Networking",
            "IAgentBus — local pub/sub. INetworkBus — cross-node events. " +
            "BrickCatalog — distributed capability discovery. " +
            "Wire protocol WireFormat 2025-01. RemoteBrick proxies. " +
            "Usage-weighted: frequently-used connections strengthen, unused prune. " +
            "Five phases: synaptic plasticity → signal propagation → memory consolidation → emergent coordination → self-organization.",
            new[] { "network", "node", "neuroplastic", "share", "distribute", "instance" }),

        ["use_cases"] = new("Use Cases",
            "Game AI — self-extending NPC behaviors. " +
            "Self-healing infrastructure — fixes become shared RemoteBricks. " +
            "Defense/classified — offline AI in air-gapped environments. " +
            "IoT/edge — agents on devices learning locally. " +
            "Data pipelines — autonomous monitoring/repair. " +
            "Knowledge retention — institutional knowledge survives staff turnover.",
            new[] { "build", "use.?case", "example", "application", "game", "defense", "pipeline" }),

        ["this_guide"] = new("This Guide (Meta)",
            "This app dog-foods the Nexo stack: IBehaviorExecutor + GuideChatBrick (domain Brick) for chat; RenderBehavior for visuals; 3D commands pushed to IUnitySceneSink for Unity consumption. " +
            "Agent creates all visuals using rendering bricks — nothing pre-built. " +
            "Sandboxed: only rendering + chat bricks allowed. " +
            "Ephemeral: close app, everything disappears. " +
            "Running locally via Ollama. Product demo IS the product.",
            new[] { "this", "guide", "demo", "meta", "itself", "sandbox", "ephemeral" })
    };

    public string GetRelevantContext(string query)
    {
        var lower = query.ToLowerInvariant();
        var matched = _sections
            .Where(s => s.Value.Keywords.Any(k =>
                Regex.IsMatch(lower, k)))
            .Select(s => $"## {s.Value.Title}\n{s.Value.Content}")
            .ToList();
        if (matched.Count == 0)
            matched.Add($"## {_sections["core"].Title}\n{_sections["core"].Content}");
        return string.Join("\n\n", matched);
    }
}

public record KnowledgeSection(string Title, string Content, string[] Keywords);
