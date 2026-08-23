using System.Text.Json;
using GameDirector.Mcp.Tools;

namespace GameDirector.Mcp;

/// <summary>Contract for mcp tool.</summary>
public interface IMcpTool
{
    /// <summary>Name.</summary>
    string Name { get; }
    /// <summary>Description.</summary>
    string Description { get; }
    /// <summary>Input schema.</summary>
    JsonElement InputSchema { get; }
    /// <summary>Execute async.</summary>
    /// <param name="arguments">Arguments.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<JsonElement> ExecuteAsync(JsonElement arguments, CancellationToken ct);
}
