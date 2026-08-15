using System.Text.Json;
using ModelContextProtocol.Protocol;
using Nexo.Abstractions;

namespace Nexo.Mcp.Client;

/// <summary>
/// Pure conversion helpers between Nexo tool-call shapes and MCP client shapes.
/// Kept static and side-effect free so the mapping rules are unit-testable without a live
/// connection (internal + InternalsVisibleTo for the test satellite).
/// </summary>
internal static class McpJsonBridge
{
    /// <summary>
    /// Converts a Nexo <see cref="ToolCall.Arguments"/> element into the argument dictionary the
    /// MCP client API takes. Values are cloned — the source element's backing document is owned
    /// by the caller and may be disposed before the request is serialized. Non-object arguments
    /// map to an empty dictionary (MCP tool arguments are object-shaped by spec).
    /// </summary>
    public static IReadOnlyDictionary<string, object?> ToArgumentDictionary(JsonElement arguments)
    {
        if (arguments.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, object?>(StringComparer.Ordinal);
        }

        var dictionary = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in arguments.EnumerateObject())
        {
            dictionary[property.Name] = property.Value.Clone();
        }

        return dictionary;
    }

    /// <summary>
    /// Maps a remote <see cref="CallToolResult"/> onto a Nexo <see cref="ToolResult"/>:
    /// text blocks join into the log/text representation; structured content (when present)
    /// becomes the payload; remote <c>isError</c> becomes an <see cref="McpToolFailure"/> payload
    /// per the repo convention of error-in-payload rather than exceptions.
    /// </summary>
    public static ToolResult ToToolResult(CallToolResult result, string toolId, int tick)
    {
        var text = string.Join(
            "\n",
            result.Content.OfType<TextContentBlock>().Select(block => block.Text));

        if (result.IsError == true)
        {
            var error = string.IsNullOrWhiteSpace(text) ? "remote tool reported an error" : text;
            return new ToolResult(
                new McpProxyDelta(tick, $"{toolId} error"),
                new McpToolFailure(error));
        }

        object? payload = result.StructuredContent.HasValue
            ? result.StructuredContent.Value.Clone()
            : text;

        return new ToolResult(new McpProxyDelta(tick, $"{toolId} ok"), payload);
    }
}
