using System.Text.Json;
using Microsoft.Extensions.Logging;
using Ashlar.BackgroundAgents.RAG;

namespace Ashlar.CLI.Commands.BackgroundAgent;

/// <summary>
/// CLI command handler for RAG operations (index, search, stats, clear).
/// </summary>
public class RAGCommand
{
    private readonly IRAGService _ragService;
    private readonly IKnowledgeBaseIndexer _indexer;
    private readonly ILogger<RAGCommand> _logger;

    /// <summary>Creates a new RAGCommand instance.</summary>
    public RAGCommand(
        IRAGService ragService,
        IKnowledgeBaseIndexer indexer,
        ILogger<RAGCommand> logger)
    {
        _ragService = ragService ?? throw new ArgumentNullException(nameof(ragService));
        _indexer = indexer ?? throw new ArgumentNullException(nameof(indexer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Index files or directories into the RAG store.
    /// </summary>
    public async Task<int> IndexAsync(IEnumerable<string> paths, string? sensitivity, bool formatJson, CancellationToken ct = default)
    {
        try
        {
            var pathList = paths?.Where(p => !string.IsNullOrWhiteSpace(p)).ToList() ?? new List<string>();
            if (pathList.Count == 0)
            {
                if (formatJson)
                    Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = "At least one path required" }));
                else
                    Console.Error.WriteLine("At least one path required");
                return 1;
            }
            var count = await _indexer.IndexDocumentsAsync(pathList, sensitivity, ct).ConfigureAwait(false);
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, paths = pathList, indexedCount = count }));
            else
                Console.Out.WriteLine($"Indexed {count} document(s) from {pathList.Count} path(s).");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RAG index failed");
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
            else
                Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    /// <summary>
    /// Search the RAG store.
    /// </summary>
    public async Task<int> SearchAsync(string query, int maxResults, double minScore, string? maxSensitivity, bool formatJson, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                if (formatJson)
                    Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = "Query required" }));
                else
                    Console.Error.WriteLine("Query required");
                return 1;
            }
            var results = await _ragService.SearchAsync(query, maxResults, minScore, maxSensitivity, ct).ConfigureAwait(false);
            if (formatJson)
            {
                var items = results.Select(r => new { r.Id, r.Text, r.Score, r.SensitivityLevelName }).ToList();
                var options = new JsonSerializerOptions { WriteIndented = true };
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, query, results = items }, options));
            }
            else
            {
                Console.Out.WriteLine($"RAG Search: \"{query}\"");
                foreach (var r in results)
                {
                    Console.Out.WriteLine($"  Score: {r.Score:F3} | {r.SensitivityLevelName ?? "-"} | {r.Id}");
                    Console.Out.WriteLine($"    {Truncate(r.Text, 120)}");
                }
            }
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RAG search failed");
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
            else
                Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    /// <summary>
    /// Show RAG store statistics (document count).
    /// </summary>
    public async Task<int> StatsAsync(bool formatJson, CancellationToken ct = default)
    {
        try
        {
            var count = await _ragService.GetDocumentCountAsync(ct).ConfigureAwait(false);
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, documentCount = count }));
            else
                Console.Out.WriteLine($"RAG store document count: {count}");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RAG stats failed");
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
            else
                Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    /// <summary>
    /// Clear all documents from the RAG store.
    /// </summary>
    public async Task<int> ClearAsync(bool formatJson, CancellationToken ct = default)
    {
        try
        {
            await _ragService.ClearAsync(ct).ConfigureAwait(false);
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, action = "cleared" }));
            else
                Console.Out.WriteLine("RAG store cleared.");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RAG clear failed");
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
            else
                Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static string Truncate(string text, int maxLen)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        text = text.Replace("\r", " ").Replace("\n", " ");
        return text.Length <= maxLen ? text : text[..maxLen] + "...";
    }
}
