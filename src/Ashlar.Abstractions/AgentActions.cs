using System.Text.Json;

namespace Ashlar.Abstractions;

/// <summary>
/// Represents actions that an agent wants to take.
///
/// Contains a list of tool calls that the agent proposes to execute.
/// Provides a static None property for agents that don't want to take any actions.
/// </summary>
/// <param name="ToolCalls">List of tool calls the agent wants to execute.</param>
public sealed record AgentActions(IReadOnlyList<ToolCall> ToolCalls)
{
    /// <summary>
    /// Represents no actions (empty tool call list).
    /// </summary>
    public static AgentActions None { get; } = new AgentActions(Array.Empty<ToolCall>());
}
