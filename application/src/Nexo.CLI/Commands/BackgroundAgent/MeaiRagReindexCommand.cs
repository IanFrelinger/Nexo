using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.AI.Pipeline.Rag;

namespace Nexo.CLI.Commands.BackgroundAgent;

/// <summary>
/// Migrates file content into the MEAI VectorData store.
/// Hosting registers VectorData as the default <c>IRAGService</c> (Phase 6).
/// </summary>
public sealed class MeaiRagReindexCommand
{
    private readonly VectorDataRagService _rag;
    private readonly ILogger<MeaiRagReindexCommand> _logger;

    /// <summary>Creates the MEAI RAG reindex command.</summary>
    public MeaiRagReindexCommand(VectorDataRagService rag, ILogger<MeaiRagReindexCommand> logger)
    {
        _rag = rag ?? throw new ArgumentNullException(nameof(rag));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Reads files/directories and re-indexes into the VectorData store.
    /// </summary>
    public async Task<int> ReindexAsync(
        IEnumerable<string> paths,
        string? trustTier,
        bool formatJson,
        CancellationToken ct = default)
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

            var tier = string.IsNullOrWhiteSpace(trustTier) ? "Public" : trustTier.Trim();
            var chunks = new List<(string Key, string Text, string? SourceUri, string TrustTier)>();
            foreach (var path in pathList)
            {
                ct.ThrowIfCancellationRequested();
                CollectFiles(path, chunks, tier);
            }

            var count = await _rag.ReindexAsync(chunks, ct).ConfigureAwait(false);
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, indexedCount = count, trustTier = tier }));
            else
                Console.Out.WriteLine($"Re-indexed {count} chunk(s) into MEAI VectorData store (trust tier={tier}). Legacy store unchanged.");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MEAI RAG reindex failed");
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
            else
                Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static void CollectFiles(
        string path,
        List<(string Key, string Text, string? SourceUri, string TrustTier)> chunks,
        string trustTier)
    {
        path = Path.GetFullPath(path);
        if (File.Exists(path))
        {
            AddFile(path, chunks, trustTier);
            return;
        }

        if (!Directory.Exists(path))
        {
            throw new FileNotFoundException($"Path not found: {path}");
        }

        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        {
            AddFile(file, chunks, trustTier);
        }
    }

    private static void AddFile(
        string file,
        List<(string Key, string Text, string? SourceUri, string TrustTier)> chunks,
        string trustTier)
    {
        var text = File.ReadAllText(file);
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        chunks.Add((Key: file, Text: text, SourceUri: new Uri(file).AbsoluteUri, TrustTier: trustTier));
    }
}
