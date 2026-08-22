using System.Text.Json;

namespace Ashlar.Orchestration.Communication.Models;

/// <summary>
/// Message emitted when an agent state changes.
/// 
/// Contains the new and previous state of the agent.
/// Used to notify other agents about state transitions.
/// </summary>
public sealed record AgentStateChanged : AgentMessage
{
    public AgentStateChanged()
    {
        MessageType = "AgentStateChanged";
    }

    /// <summary>
    /// The new state of the agent.
    /// </summary>
    public required string NewState { get; init; }

    /// <summary>
    /// The previous state of the agent.
    /// </summary>
    public string? PreviousState { get; init; }
}
