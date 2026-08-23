using System.Text.Json;
using GameDirector.Mcp;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace GameDirector.Mcp.Tools;

/// <summary>Analyze balance tool.</summary>
public sealed class AnalyzeBalanceTool : IMcpTool
{
    private readonly McpBrickExecutor _executor;

    /// <summary>Analyze balance tool.</summary>
    /// <param name="executor">Executor.</param>
    public AnalyzeBalanceTool(McpBrickExecutor executor) => _executor = executor;

    public string Name => "analyze_balance";
    public string Description => "Ingest a weapon or unit stat sheet and return outlier flags, TTK spread, and suggested deltas.";
    public JsonElement InputSchema => McpToolHelpers.Schema(
        new { sheet_id = new { type = "string" } },
        new { data = new { type = "array" } },
        new { baseline_id = new { type = "string" } });

    public async Task<JsonElement> ExecuteAsync(JsonElement arguments, CancellationToken ct)
    {
        var sheetId = arguments.TryGetProperty("sheet_id", out var s) ? s.GetString() ?? "" : "";
        var baselineId = arguments.TryGetProperty("baseline_id", out var b) ? b.GetString() : null;
        object? data = arguments.TryGetProperty("data", out var d) ? d : new JsonElement[] { };

        var input = new Dictionary<string, object?>
        {
            ["sheet_id"] = sheetId,
            ["data"] = data!,
            ["baseline_id"] = baselineId
        };

        return await _executor.ExecuteAsync("balance.analysis", input, ImplementationType.Deterministic, ct)
            .ConfigureAwait(false);
    }
}
