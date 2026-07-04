using System.Text.Json;

namespace Nexo.Orchestration.Communication.Models;

/// <summary>
/// Message emitted when an agent produces output.
/// 
/// Contains the output produced by the agent and its schema identifier.
/// Used to notify dependent agents that output is available.
/// </summary>
public sealed record OutputEmitted : AgentMessage
{
    public OutputEmitted()
    {
        MessageType = "OutputEmitted";
    }

    /// <summary>
    /// The output produced by the agent.
    /// </summary>
    public required object Output { get; init; }

    /// <summary>
    /// Output schema identifier.
    /// </summary>
    public string? OutputSchema { get; init; }
}
