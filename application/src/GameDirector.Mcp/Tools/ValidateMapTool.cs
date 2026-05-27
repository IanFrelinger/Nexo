using System.Text.Json;
using GameDirector.Mcp;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace GameDirector.Mcp.Tools;

public sealed class ValidateMapTool : IMcpTool
{
    private readonly McpBrickExecutor _executor;

    public ValidateMapTool(McpBrickExecutor executor) => _executor = executor;

    public string Name => "validate_map";
    public string Description => "Run a map config through MapFlowBrick and return structural health metrics.";
    public JsonElement InputSchema => McpToolHelpers.Schema(
        new { map_id = new { type = "string" } },
        new { config = new { type = "object" } },
        new { session_id = new { type = "string" } });

    public async Task<JsonElement> ExecuteAsync(JsonElement arguments, CancellationToken ct)
    {
        var mapId = arguments.TryGetProperty("map_id", out var m) ? m.GetString() ?? "" : "";
        var sessionId = arguments.TryGetProperty("session_id", out var sid) ? sid.GetString() : null;
        if (!arguments.TryGetProperty("config", out var config))
            throw new ArgumentException("config is required");

        var input = new Dictionary<string, object?>
        {
            ["map_id"] = mapId,
            ["config"] = config,
            ["session_id"] = sessionId
        };

        return await _executor.ExecuteAsync("map.flow", input, ImplementationType.Deterministic, ct)
            .ConfigureAwait(false);
    }
}
