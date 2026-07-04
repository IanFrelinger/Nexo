using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Abstractions;

namespace Nexo.Orchestration.Models;

/// <summary>Hierarchical model runtime configuration for orchestration agents and domains.</summary>
public sealed record OrchestrationRuntimeSpec
{
    /// <summary>Default model spec applied when no agent or domain override matches.</summary>
    public ModelRuntimeSpec Model { get; init; } = new();

    /// <summary>Per-domain model overrides keyed by domain name.</summary>
    public Dictionary<string, ModelRuntimeSpec> Domains { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Per-agent model overrides keyed by agent identifier.</summary>
    public Dictionary<string, ModelRuntimeSpec> Agents { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Optional barrier level hint for downstream routing.</summary>
    public string? BarrierLevel { get; init; }

    /// <summary>Optional preferred region for model or endpoint selection.</summary>
    public string? PreferredRegion { get; init; }

    /// <summary>Resolves the effective model spec for an agent and domain, preferring agent, then domain, then default.</summary>
    public ModelRuntimeSpec Resolve(string agentId, string domain)
    {
        if (!string.IsNullOrWhiteSpace(agentId) && Agents.TryGetValue(agentId, out var a)) return a;
        if (!string.IsNullOrWhiteSpace(domain) && Domains.TryGetValue(domain, out var d)) return d;
        return Model;
    }

    /// <summary>Returns an empty runtime spec with default model preferences.</summary>
    public static OrchestrationRuntimeSpec Default() => new();
}
