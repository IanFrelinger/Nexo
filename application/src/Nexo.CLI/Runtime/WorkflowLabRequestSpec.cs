using Nexo.Orchestration.Models;

namespace Nexo.CLI.Runtime;

/// <summary>User prompt scenario exercised during workflow lab stress runs.</summary>
public sealed record WorkflowLabRequestSpec
{
    /// <summary>Stable request identifier referenced by stress runs.</summary>
    public string Id { get; init; } = "request-1";

    /// <summary>Prompt text submitted to the orchestrated workflow.</summary>
    public string Prompt { get; init; } = string.Empty;
}
