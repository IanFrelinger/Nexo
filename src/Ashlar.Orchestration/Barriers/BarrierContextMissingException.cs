namespace Ashlar.Orchestration.Barriers;

/// <summary>Thrown when barrier context is required but not established for an agent invocation.</summary>
public sealed class BarrierContextMissingException : Exception
{
    public BarrierContextMissingException(string agentName, string correlationId)
        : base($"Barrier context is required but missing for agent '{agentName}'.")
    {
        AgentName = agentName;
        CorrelationId = correlationId;
    }

    public string AgentName { get; }

    public string CorrelationId { get; }

    public string ErrorCode => "BARRIER_CONTEXT_MISSING";
}
