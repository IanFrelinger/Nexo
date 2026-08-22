using System.Text.Json;
using GameDirector.Mcp.Tools;

namespace GameDirector.Mcp;

/// <summary>Mcp tool descriptor value.</summary>
/// <param name="Name">Name.</param>
/// <param name="Description">Description.</param>
/// <param name="InputSchema">Input schema.</param>
public sealed record McpToolDescriptor(string Name, string Description, JsonElement InputSchema);
