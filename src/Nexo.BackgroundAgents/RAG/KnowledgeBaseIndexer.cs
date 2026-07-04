using Nexo.BackgroundAgents.DataSensitivity;
using Nexo.Core.Application.Trust.Ports;

namespace Nexo.BackgroundAgents.RAG;

/// <summary>
/// Implementation that reads text files and indexes them via IRAGService.
/// </summary>
public sealed class KnowledgeBaseIndexer : IKnowledgeBaseIndexer
{
    private readonly IRAGService _ragService;
    private readonly IDataSensitivityRegistry? _sensitivityRegistry;
    private readonly IObservationGate? _observationGate;
    private static readonly string[] DefaultTextExtensions = { ".txt", ".md", ".json", ".xml", ".csv", ".log" };

    /// <summary>
    /// File extensions treated as text (default: .txt, .md, .json, .xml, .csv, .log).
    /// </summary>
    public IReadOnlyList<string> TextExtensions { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="KnowledgeBaseIndexer"/> class.
    /// </summary>
    /// <param name="ragService">RAG service for indexing.</param>
    /// <param name="sensitivityRegistry">Optional registry to validate sensitivity level names.</param>
    /// <param name="observationGate">Optional gate; when present, skips files when observation is disallowed.</param>
    /// <param name="textExtensions">Optional list of file extensions to index (default: .txt, .md, etc.).</param>
    public KnowledgeBaseIndexer(
        IRAGService ragService,
        IDataSensitivityRegistry? sensitivityRegistry = null,
        IObservationGate? observationGate = null,
        IEnumerable<string>? textExtensions = null)
    {
        _ragService = ragService ?? throw new ArgumentNullException(nameof(ragService));
        _sensitivityRegistry = sensitivityRegistry;
        _observationGate = observationGate;
        TextExtensions = textExtensions?.ToList() ?? DefaultTextExtensions.ToList();
    }

    /// <inheritdoc />
    public async Task<int> IndexDocumentsAsync(
        IEnumerable<string> paths,
        string? defaultSensitivityLevelName,
        CancellationToken cancellationToken = default)
    {
        if (paths == null)
            throw new ArgumentNullException(nameof(paths));

        if (!string.IsNullOrWhiteSpace(defaultSensitivityLevelName) && _sensitivityRegistry != null)
        {
            if (_sensitivityRegistry.GetByName(defaultSensitivityLevelName) == null)
                throw new ArgumentException($"Unknown sensitivity level: {defaultSensitivityLevelName}", nameof(defaultSensitivityLevelName));
        }

        var files = GatherFiles(paths);
        var count = 0;
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_observationGate != null)
            {
                var projectPath = Path.GetDirectoryName(file);
                if (!_observationGate.ShouldObserve("file-contents", "knowledge-indexer", projectPath))
                    continue;
            }

            try
            {
                var text = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
                var id = Path.GetFullPath(file);
                await _ragService.IndexAsync(id, text, defaultSensitivityLevelName, cancellationToken).ConfigureAwait(false);
                count++;
            }
            catch (IOException)
            {
                // Skip unreadable files
            }
        }

        return count;
    }

    private List<string> GatherFiles(IEnumerable<string> paths)
    {
        var list = new List<string>();
        foreach (var path in paths)
        {
            if (string.IsNullOrWhiteSpace(path))
                continue;
            var full = Path.GetFullPath(path);
            if (File.Exists(full))
            {
                if (IsTextFile(full))
                    list.Add(full);
            }
            else if (Directory.Exists(full))
            {
                foreach (var file in Directory.EnumerateFiles(full, "*", SearchOption.AllDirectories))
                {
                    if (IsTextFile(file))
                        list.Add(file);
                }
            }
        }

        return list;
    }

    private bool IsTextFile(string path)
    {
        var ext = Path.GetExtension(path);
        return TextExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
    }
}
