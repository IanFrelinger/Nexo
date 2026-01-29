using System.Text.Json;
using Nexo.Abstractions;

namespace Nexo.BackgroundAgents.RAG;

/// <summary>
/// ITool that exposes RAG search to agents. Id: "rag_search".
/// </summary>
public sealed class RAGTool : ITool
{
    /// <summary>
    /// Default tool id.
    /// </summary>
    public const string DefaultId = "rag_search";

    private static readonly ToolSchema SchemaInstance = new(
        DefaultId,
        "Search the RAG knowledge base for documents similar to the query. Returns matching text chunks with scores.",
        """{"type":"object","properties":{"query":{"type":"string","description":"Search query text"},"maxResults":{"type":"integer","description":"Max results (default 5)"},"minScore":{"type":"number","description":"Min similarity 0-1 (default 0.7)"},"maxSensitivityLevelName":{"type":"string","description":"Only return docs at or below this sensitivity level"}},"required":["query"]}""");

    private readonly IRAGService _ragService;
    private readonly int _defaultMaxResults;
    private readonly double _defaultMinScore;

    /// <inheritdoc />
    public string Id => DefaultId;

    /// <inheritdoc />
    public ToolSchema Schema => SchemaInstance;

    /// <summary>
    /// Initializes a new instance of the <see cref="RAGTool"/> class.
    /// </summary>
    /// <param name="ragService">RAG service.</param>
    /// <param name="defaultMaxResults">Default max results when not specified (default 5).</param>
    /// <param name="defaultMinScore">Default min score when not specified (default 0.7).</param>
    public RAGTool(IRAGService ragService, int defaultMaxResults = 5, double defaultMinScore = 0.7)
    {
        _ragService = ragService ?? throw new ArgumentNullException(nameof(ragService));
        _defaultMaxResults = defaultMaxResults;
        _defaultMinScore = defaultMinScore;
    }

    /// <inheritdoc />
    public async Task<ToolResult> InvokeAsync(ToolCall toolCall, WorldSnapshot s, CancellationToken ct)
    {
        var args = ParseArgs(toolCall);
        var query = args.Query ?? string.Empty;
        var maxResults = args.MaxResults ?? _defaultMaxResults;
        var minScore = args.MinScore ?? _defaultMinScore;
        var maxSensitivity = args.MaxSensitivityLevelName;
        if (string.IsNullOrEmpty(maxSensitivity) && s.Data.TryGetValue("maxDataSensitivity", out var levelObj) && levelObj is string levelName)
            maxSensitivity = levelName;

        var results = await _ragService.SearchAsync(query, maxResults, minScore, maxSensitivity, ct).ConfigureAwait(false);
        var tick = s.Tick;
        var log = new[] { $"RAG search: query='{query}', results={results.Count}" };
        var delta = new ActionDelta(tick, tick + 1, log);
        var payload = results.Select(r => new { r.Id, r.Text, r.Score, r.SensitivityLevelName }).ToList();
        return new ToolResult(delta, payload);
    }

    private static RAGSearchArgs ParseArgs(ToolCall call)
    {
        try
        {
            var json = call.Arguments.GetRawText();
            return JsonSerializer.Deserialize<RAGSearchArgs>(json) ?? new RAGSearchArgs();
        }
        catch
        {
            return new RAGSearchArgs();
        }
    }

    private sealed class RAGSearchArgs
    {
        [System.Text.Json.Serialization.JsonPropertyName("query")]
        public string? Query { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("maxResults")]
        public int? MaxResults { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("minScore")]
        public double? MinScore { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("maxSensitivityLevelName")]
        public string? MaxSensitivityLevelName { get; set; }
    }
}
