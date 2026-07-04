using System.Text.Json;
using GameDirector.Mcp;
using Nexo.Core.Domain.Bricks;

namespace GameDirector.Mcp.Tools;

/// <summary>Generate content tool.</summary>
public sealed class GenerateContentTool : IMcpTool
{
    private readonly McpBrickExecutor _executor;

    /// <summary>Generate content tool.</summary>
    /// <param name="executor">Executor.</param>
    public GenerateContentTool(McpBrickExecutor executor) => _executor = executor;

    public string Name => "generate_content";
    public string Description => "Generate flavor text, item descriptions, or macro variants using Forge session context.";
    public JsonElement InputSchema => McpToolHelpers.Schema(
        new { session_id = new { type = "string" } },
        new { prompt = new { type = "string" } },
        new { target_type = new { type = "string", @enum = new[] { "flavor", "item", "macro" } } },
        new { fast = new { type = "boolean" } });

    public async Task<JsonElement> ExecuteAsync(JsonElement arguments, CancellationToken ct)
    {
        var sessionId = arguments.TryGetProperty("session_id", out var s) ? s.GetString() ?? "" : "";
        var prompt = arguments.TryGetProperty("prompt", out var p) ? p.GetString() ?? "" : "";
        var targetType = arguments.TryGetProperty("target_type", out var t) ? t.GetString() ?? "flavor" : "flavor";
        var fast = arguments.TryGetProperty("fast", out var f) && f.GetBoolean();

        var input = new Dictionary<string, object?>
        {
            ["session_id"] = sessionId,
            ["prompt"] = prompt,
            ["target_type"] = targetType,
            ["fast"] = fast
        };

        return await _executor.ExecuteAsync("content.generation", input, ImplementationType.Agentic, ct)
            .ConfigureAwait(false);
    }
}
