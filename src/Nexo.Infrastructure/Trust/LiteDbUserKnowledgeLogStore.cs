using LiteDB;
using Nexo.Core.Application.Trust.Models;
using Nexo.Core.Application.Trust.Ports;

namespace Nexo.Infrastructure.Trust;

/// <summary>
/// LiteDB-backed user knowledge log. Pure managed C#, runs on all platforms (macOS, Linux, Windows, etc.).
/// Persists entries with provenance and versioning.
/// </summary>
public sealed class LiteDbUserKnowledgeLogStore : IUserKnowledgeLogStore
{
    private readonly string _connectionString;

    private const string CollectionName = "user_knowledge_log";

    /// <summary>
    /// Creates a new LiteDB-backed user knowledge log store.
    /// </summary>
    /// <param name="pathOrConnectionString">File path (e.g. knowledge.db) or LiteDB connection string (e.g. "Filename=knowledge.db").</param>
    public LiteDbUserKnowledgeLogStore(string pathOrConnectionString)
    {
        if (string.IsNullOrWhiteSpace(pathOrConnectionString))
            throw new ArgumentNullException(nameof(pathOrConnectionString));

        var trimmed = pathOrConnectionString.Trim();
        _connectionString = trimmed.StartsWith("Filename=", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : $"Filename={trimmed}";
    }

    /// <inheritdoc />
    public Task UpsertAsync(UserKnowledgeLogEntry entry, CancellationToken cancellationToken = default)
    {
        using var db = new LiteDatabase(_connectionString);
        var col = db.GetCollection<KnowledgeLogDoc>(CollectionName);
        EnsureIndex(col);

        var existing = col.FindById(entry.Id);
        var version = existing != null ? existing.Version + 1 : entry.Version;
        var createdAt = existing?.CreatedAt ?? entry.CreatedAt;

        var doc = new KnowledgeLogDoc
        {
            Id = entry.Id,
            DataType = entry.DataType ?? string.Empty,
            Content = entry.Content ?? string.Empty,
            SourceObservationIds = entry.SourceObservationIds?.ToArray() ?? Array.Empty<string>(),
            Version = version,
            CreatedAt = createdAt,
            UpdatedAt = DateTimeOffset.UtcNow,
            DeletedAt = null,
        };
        col.Upsert(doc);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        using var db = new LiteDatabase(_connectionString);
        var col = db.GetCollection<KnowledgeLogDoc>(CollectionName);
        var doc = col.FindById(id);
        if (doc != null)
        {
            doc.DeletedAt = DateTimeOffset.UtcNow;
            doc.UpdatedAt = DateTimeOffset.UtcNow;
            col.Update(doc);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<UserKnowledgeLogEntry?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        using var db = new LiteDatabase(_connectionString);
        var col = db.GetCollection<KnowledgeLogDoc>(CollectionName);
        var doc = col.FindOne(x => x.Id == id && x.DeletedAt == null);
        if (doc == null) return Task.FromResult<UserKnowledgeLogEntry?>(null);
        return Task.FromResult<UserKnowledgeLogEntry?>(ToEntry(doc));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<UserKnowledgeLogEntry>> GetAsync(string? dataType = null, int maxCount = 100, CancellationToken cancellationToken = default)
    {
        using var db = new LiteDatabase(_connectionString);
        var col = db.GetCollection<KnowledgeLogDoc>(CollectionName);
        var query = col.Query().Where(x => x.DeletedAt == null);
        if (!string.IsNullOrEmpty(dataType))
            query = query.Where(x => x.DataType == dataType);
        var docs = query.OrderByDescending(x => x.UpdatedAt).Limit(maxCount).ToList();
        var entries = docs.Select(ToEntry).ToList();
        return Task.FromResult<IReadOnlyList<UserKnowledgeLogEntry>>(entries);
    }

    /// <inheritdoc />
    public async Task<string> ExportToJsonAsync(int maxCount = 1000, CancellationToken cancellationToken = default)
    {
        var entries = await GetAsync(null, maxCount, cancellationToken).ConfigureAwait(false);
        return UserKnowledgeLogExportHelper.ToJson(entries);
    }

    /// <inheritdoc />
    public async Task<string> ExportToMarkdownAsync(int maxCount = 1000, CancellationToken cancellationToken = default)
    {
        var entries = await GetAsync(null, maxCount, cancellationToken).ConfigureAwait(false);
        return UserKnowledgeLogExportHelper.ToMarkdown(entries);
    }

    private static void EnsureIndex(ILiteCollection<KnowledgeLogDoc> col)
    {
        col.EnsureIndex(x => x.UpdatedAt);
        col.EnsureIndex(x => x.DataType);
    }

    private static UserKnowledgeLogEntry ToEntry(KnowledgeLogDoc doc)
    {
        return new UserKnowledgeLogEntry
        {
            Id = doc.Id,
            DataType = doc.DataType,
            Content = doc.Content,
            SourceObservationIds = doc.SourceObservationIds ?? Array.Empty<string>(),
            Version = doc.Version,
            CreatedAt = doc.CreatedAt,
            UpdatedAt = doc.UpdatedAt,
            DeletedAt = doc.DeletedAt,
        };
    }

    private sealed class KnowledgeLogDoc
    {
        [BsonId]
        public string Id { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string[]? SourceObservationIds { get; set; }
        public int Version { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
    }
}
