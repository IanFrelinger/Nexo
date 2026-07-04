using Nexo.Orchestration.Models;

namespace Nexo.CLI.Runtime;

/// <summary>Single agent role within a workflow lab composition.</summary>
public sealed record WorkflowLabAgentRoleSpec
{
    /// <summary>Stable agent identifier within the composition.</summary>
    public string AgentId { get; init; } = string.Empty;

    /// <summary>Role label (e.g. planner, builder, qa-functional).</summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>Domain pack or specialty lane for the agent.</summary>
    public string Domain { get; init; } = "general";

    /// <summary>Objective statement assigned to this agent for the scenario.</summary>
    public string Goal { get; init; } = string.Empty;

    /// <summary>Optional operator-facing description of the role.</summary>
    public string? Description { get; init; }

    /// <summary>Optional cluster or squad identifier for grouping agents.</summary>
    public string? ClusterId { get; init; }

    /// <summary>Agent id this role reports to in a hierarchy, if any.</summary>
    public string? ReportsToAgentId { get; init; }

    /// <summary>Ordered command chain from root planner to this agent.</summary>
    public IReadOnlyList<string> CommandChain { get; init; } = Array.Empty<string>();

    /// <summary>Preferred Ollama model name for this agent.</summary>
    public string? OllamaModel { get; init; }

    /// <summary>Execution provider override for this agent.</summary>
    public string? Provider { get; init; }

    /// <summary>Model preference hint (agentic, deterministic, auto).</summary>
    public string? Prefer { get; init; }
}
