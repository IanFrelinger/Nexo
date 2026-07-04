using System.Text.Json;

namespace Nexo.Abstractions;

/// <summary>
/// Interface for tools that agents can invoke to perform actions.
///
/// Tools are capabilities that agents can use to interact with the world.
/// Each tool has:
/// - A unique identifier
/// - A schema describing its inputs
/// - An invocation method that takes a tool call and world state
///
/// Tools are registered with IToolbox and can be discovered by agents.
/// </summary>
public interface ITool
{
    /// <summary>
    /// Unique identifier for this tool.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Schema describing this tool's inputs and behavior.
    /// </summary>
    ToolSchema Schema { get; }

    /// <summary>
    /// Invokes the tool with the given call and world state.
    /// </summary>
    /// <param name="toolCall">The tool call containing arguments.</param>
    /// <param name="s">The current world snapshot.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tool result containing the action delta and optional payload.</returns>
    Task<ToolResult> InvokeAsync(ToolCall toolCall, WorldSnapshot s, CancellationToken ct);
}
