namespace Nexo.BrickContracts;

/// <summary>
/// Wire DTO for execution context (agent, behavior, air-gap, etc.).
/// </summary>
public class ExecutionContextDto
{
    public string? CorrelationId { get; set; }
    public string? AgentId { get; set; }
    public string? BehaviorId { get; set; }
    public bool IsAirGapped { get; set; }
    public bool AuditMode { get; set; }
    public string? Provider { get; set; }
    /// <summary>Variables from previous steps (key-value, JSON-serializable).</summary>
    public IReadOnlyDictionary<string, object>? Variables { get; set; }
}
