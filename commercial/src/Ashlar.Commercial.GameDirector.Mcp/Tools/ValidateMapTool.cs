using System.Text.Json;
using GameDirector.Mcp;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace GameDirector.Mcp.Tools;

/// <summary>Validate map tool.</summary>
public sealed class ValidateMapTool : IMcpTool
{
    private readonly McpBrickExecutor _executor;

    /// <summary>Validate map tool.</summary>
    /// <param name="executor">Executor.</param>
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
            /// <summary>Argument exception.</summary>
            /// <param name="required"">Required".</param>
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
