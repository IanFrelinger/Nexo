using System.Text.Json;

namespace Ashlar.Abstractions;

/// <summary>
/// Result of a tool invocation.
///
/// Contains:
/// - Action delta representing the world state change
/// - Optional payload with additional data
///
/// Returned by ITool.InvokeAsync after tool execution.
/// </summary>
/// <param name="Delta">The action delta representing the world state change.</param>
/// <param name="Payload">Optional payload with additional tool-specific data.</param>
public sealed record ToolResult(IActionDelta Delta, object? Payload);
