using Nexo.Orchestration.Models;

namespace Nexo.CLI.Runtime;

/// <summary>Model routing profile for a workflow lab benchmark scenario.</summary>
public sealed record WorkflowLabModelProfileSpec
{
    /// <summary>Stable profile identifier referenced by stress runs.</summary>
    public string Id { get; init; } = "profile-1";

    /// <summary>Human-readable profile summary for operator reports.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Default model runtime settings when no domain or agent override matches.</summary>
    public ModelRuntimeSpec Default { get; init; } = new();

    /// <summary>Domain-specific model runtime overrides keyed by domain name.</summary>
    public Dictionary<string, ModelRuntimeSpec> Domains { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Agent-specific model runtime overrides keyed by agent id.</summary>
    public Dictionary<string, ModelRuntimeSpec> Agents { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Legacy default model name hint when runtime spec omits provider details.</summary>
    public string? DefaultModelName { get; init; }

    /// <summary>Legacy domain-to-model-name hints keyed by domain name.</summary>
    public Dictionary<string, string> DomainModelNames { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Legacy agent-to-model-name hints keyed by agent id.</summary>
    public Dictionary<string, string> AgentModelHints { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
