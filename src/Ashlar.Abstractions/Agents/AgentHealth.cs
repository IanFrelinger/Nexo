namespace Ashlar.Abstractions.Agents;

/// <summary>
/// Indicates that the orchestration agent is fully operational and in a normal, healthy state.
/// </summary>
public enum AgentHealth
{
    /// <summary>
    /// Represents a state where the agent is operating normally and is in good health,
    /// with all systems functioning as expected and no known issues present.
    /// </summary>
    Healthy,

    /// <summary>
    /// Indicates that the agent's functionality is impaired or degraded, potentially impacting performance or reliability.
    /// </summary>
    Degraded,

    /// <summary>
    /// Indicates that the orchestration agent is in an unhealthy state,
    /// likely unable to perform its intended operations or may require intervention.
    /// </summary>
    Unhealthy
}
