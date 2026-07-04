namespace Nexo.Core.Domain.Agents;

/// <summary>
/// Operational constraints for an agent.
/// </summary>
public class AgentConstraints
{
    /// <summary>Maximum wall-clock time allowed for a single behavior execution.</summary>
    public TimeSpan MaxExecutionTime { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>Maximum behaviors that may run concurrently for this agent.</summary>
    public int MaxConcurrentBehaviors { get; init; } = 3;

    /// <summary>Rules that trigger escalation actions when conditions match.</summary>
    public IReadOnlyList<EscalationRule> EscalationRules { get; init; } = [];

    /// <summary>Granted permission identifiers for tool and policy evaluation.</summary>
    public IReadOnlyList<string> Permissions { get; init; } = [];
}
