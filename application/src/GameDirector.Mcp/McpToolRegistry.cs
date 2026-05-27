using System.Text.Json;
using GameDirector.Mcp.Tools;

namespace GameDirector.Mcp;

public sealed class McpToolRegistry
{
    private readonly IReadOnlyList<IMcpTool> _tools;

    public McpToolRegistry(
        AnalyzeBalanceTool analyzeBalance,
        ValidateMapTool validateMap,
        GenerateContentTool generateContent,
        GetAuditTrailTool getAuditTrail,
        QueryPatternsTool queryPatterns)
    {
        _tools =
        [
            analyzeBalance,
            validateMap,
            generateContent,
            getAuditTrail,
            queryPatterns
        ];
    }

    public IReadOnlyList<McpToolDescriptor> ListTools() =>
        _tools.Select(t => new McpToolDescriptor(t.Name, t.Description, t.InputSchema)).ToList();

    public async Task<JsonElement> CallAsync(string name, JsonElement? arguments, CancellationToken ct)
    {
        var tool = _tools.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Unknown tool: {name}");
        return await tool.ExecuteAsync(arguments ?? default, ct).ConfigureAwait(false);
    }
}

public sealed record McpToolDescriptor(string Name, string Description, JsonElement InputSchema);

public interface IMcpTool
{
    string Name { get; }
    string Description { get; }
    JsonElement InputSchema { get; }
    Task<JsonElement> ExecuteAsync(JsonElement arguments, CancellationToken ct);
}
