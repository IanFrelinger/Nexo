using Ashlar.Core.Domain.Execution;

namespace Ashlar.Core.Application.Workflows;

/// <summary>Simple <see cref="IExecutionContext"/> used for workflow-level brick execution.</summary>
internal class SimpleExecutionContext : IExecutionContext
{
    /// <inheritdoc />
    public string AgentId { get; init; } = "";

    /// <inheritdoc />
    public string BehaviorId { get; init; } = "";

    /// <inheritdoc />
    public bool IsAirGapped { get; init; }

    /// <inheritdoc />
    public bool AuditMode { get; init; }

    /// <inheritdoc />
    public string Provider { get; init; } = "openai";

    /// <summary>Mutable workflow variables exposed to brick input mapping.</summary>
    public Dictionary<string, object> Variables { get; init; } = new();

    /// <inheritdoc />
    IReadOnlyDictionary<string, object> IExecutionContext.Variables => Variables;
}
