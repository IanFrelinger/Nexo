using Nexo.Orchestration.Models;

namespace Nexo.CLI.Runtime;

/// <summary>Agent roster defining a workflow lab orchestration composition.</summary>
public sealed record WorkflowLabCompositionSpec
{
    /// <summary>Stable composition identifier referenced by stress runs.</summary>
    public string Id { get; init; } = "composition-1";

    /// <summary>Human-readable composition summary for operator reports.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Agent roles that participate in this composition.</summary>
    public IReadOnlyList<WorkflowLabAgentRoleSpec> Roles { get; init; } = Array.Empty<WorkflowLabAgentRoleSpec>();
}
