using System.Text.Json;
using GameDirector.Mcp.Tools;

namespace GameDirector.Mcp;

/// <summary>Mcp tool registry.</summary>
public sealed class McpToolRegistry
{
    private readonly IReadOnlyList<IMcpTool> _tools;

    public McpToolRegistry(
        AnalyzeBalanceTool analyzeBalance,
        ValidateMapTool validateMap,
        GenerateContentTool generateContent,
        GetAuditTrailTool getAuditTrail,
        QueryPatternsTool queryPatterns,
        RunBrPlaytestTool? runBrPlaytest = null,
        GetBrPlaytestReportTool? getBrPlaytestReport = null)
    {
        var tools = new List<IMcpTool>
        {
            analyzeBalance,
            validateMap,
            generateContent,
            getAuditTrail,
            queryPatterns
        };
        if (runBrPlaytest is not null)
            tools.Add(runBrPlaytest);
        if (getBrPlaytestReport is not null)
            tools.Add(getBrPlaytestReport);
        _tools = tools;
    }

    /// <summary>List tools.</summary>
    public IReadOnlyList<McpToolDescriptor> ListTools() =>
        _tools.Select(t => new McpToolDescriptor(t.Name, t.Description, t.InputSchema)).ToList();

    public async Task<JsonElement> CallAsync(string name, JsonElement? arguments, CancellationToken ct)
    {
        var tool = _tools.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase))
            /// <summary>Invalid operation exception.</summary>
            /// <param name="{name}"">{name}".</param>
            ?? throw new InvalidOperationException($"Unknown tool: {name}");
        return await tool.ExecuteAsync(arguments ?? default, ct).ConfigureAwait(false);
    }
}
