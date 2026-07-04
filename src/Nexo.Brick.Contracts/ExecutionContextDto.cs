namespace Nexo.Brick.Contracts;
/// <summary>
/// Wire DTO for execution context (agent, behavior, air-gap, etc.).
/// </summary>
public class ExecutionContextDto
{
    /// <summary>Correlation id propagated across workflow steps and transport hops.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Executing agent id, when the brick runs inside an agent workflow.</summary>
    public string? AgentId { get; set; }

    /// <summary>Active behavior id within the agent, when applicable.</summary>
    public string? BehaviorId { get; set; }

    /// <summary>When true, outbound network and cloud provider calls are blocked.</summary>
    public bool IsAirGapped { get; set; }

    /// <summary>When true, emit extended audit telemetry for compliance review.</summary>
    public bool AuditMode { get; set; }

    /// <summary>Preferred or resolved LLM provider name for agentic implementations.</summary>
    public string? Provider { get; set; }

    /// <summary>Variables from previous steps (key-value, JSON-serializable).</summary>
    public IReadOnlyDictionary<string, object>? Variables { get; set; }
}
