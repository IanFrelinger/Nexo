using System.Text.Json;

namespace Ashlar.Abstractions;

/// <summary>
/// Interface for tool registry and memory provider.
///
/// Provides:
/// - Tool schemas for discovery
/// - Tool invocation capability
/// - Agent memory instances
///
/// Used by agents to access tools and memory.
/// </summary>
public interface IToolbox
{
    /// <summary>Returns schemas for all tools registered in this toolbox.</summary>
    IEnumerable<ToolSchema> Schemas();

    /// <summary>
    /// Invokes a tool by call id, passing the current world snapshot for context.
    /// </summary>
    /// <param name="toolCall">Tool identifier and serialized arguments.</param>
    /// <param name="s">Current world state snapshot.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Action delta and optional tool-specific payload.</returns>
    Task<ToolResult> InvokeAsync(ToolCall toolCall, WorldSnapshot s, CancellationToken ct);

    /// <summary>Returns a dedicated memory store scoped to the given agent.</summary>
    /// <param name="agent">Agent requesting memory access.</param>
    IAgentMemory MemoryFor(IAgent agent);
}
