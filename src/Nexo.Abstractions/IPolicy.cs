using System.Text.Json;

namespace Nexo.Abstractions;

/// <summary>
/// Interface for policies that approve or reject tool calls.
///
/// Policies enforce security and operational constraints by evaluating
/// tool calls against the current world state.
/// </summary>
public interface IPolicy
{
    /// <summary>
    /// Evaluates whether a proposed tool call is permitted under the current world state.
    /// </summary>
    /// <param name="toolCall">Proposed tool invocation.</param>
    /// <param name="s">Current world snapshot used for policy evaluation.</param>
    /// <param name="reason">Human-readable explanation when approval is denied.</param>
    /// <returns><see langword="true"/> if the tool call is approved; otherwise <see langword="false"/>.</returns>
    bool Approve(ToolCall toolCall, WorldSnapshot s, out string reason);
}
