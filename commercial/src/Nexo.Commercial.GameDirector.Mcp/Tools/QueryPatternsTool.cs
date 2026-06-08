using System.Text.Json;
using GameDirector.Mcp;
using Nexo.Client;

namespace GameDirector.Mcp.Tools;

public sealed class QueryPatternsTool : IMcpTool
{
    private readonly INexoClient _client;

    public QueryPatternsTool(INexoClient client) => _client = client;

    public string Name => "query_patterns";
    public string Description => "Query the Nexo pattern store for learned insights.";
    public JsonElement InputSchema => McpToolHelpers.Schema(
        new { context = new { type = "string" } },
        new { asset_type = new { type = "string", @enum = new[] { "balance", "map", "content" } } },
        new { limit = new { type = "integer" } });

    public async Task<JsonElement> ExecuteAsync(JsonElement arguments, CancellationToken ct)
    {
        var context = arguments.TryGetProperty("context", out var c) ? c.GetString() ?? "" : "";
        var assetType = arguments.TryGetProperty("asset_type", out var a) ? a.GetString() : null;
        var limit = arguments.TryGetProperty("limit", out var l) && l.TryGetInt32(out var n) ? n : 10;

        var query = $"api/knowledge/query?maxCount={limit}&dataType={Uri.EscapeDataString(context)}";
        if (!string.IsNullOrWhiteSpace(assetType))
            query += $"&eventType={Uri.EscapeDataString(assetType)}";

        var response = await _client.QueryKnowledgeAsync(query, ct).ConfigureAwait(false);
        return response;
    }
}
