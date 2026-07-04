using LiteDB;
using Nexo.Core.Application.Observation.Models;
using Nexo.Core.Application.Observation.Ports;

namespace Nexo.Infrastructure.Observation;
/// <summary>
/// LiteDB-backed pattern store. Persistent, queryable, survives restarts.
/// </summary>
public sealed class LiteDbPatternStore : IPatternStore
{
    private const string CollectionName = "observed_patterns";
    private readonly string _connectionString;
    /// <summary>
    /// Creates a new LiteDB-backed pattern store.
    /// </summary>
    /// <param name = "pathOrConnectionString">File path (e.g. patterns.db) or LiteDB connection string.</param>
    public LiteDbPatternStore(string pathOrConnectionString)
    {
        if (string.IsNullOrWhiteSpace(pathOrConnectionString))
            throw new ArgumentNullException(nameof(pathOrConnectionString));
        var trimmed = pathOrConnectionString.Trim();
        _connectionString = trimmed.StartsWith("Filename=", StringComparison.OrdinalIgnoreCase) ? trimmed : $"Filename={trimmed}";
    }

    /// <inheritdoc/>
    public Task AddAsync(ObservedPattern pattern, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var db = new LiteDatabase(_connectionString);
        var col = db.GetCollection<PatternDoc>(CollectionName);
        EnsureIndexes(col);
        var doc = ToDoc(pattern);
        col.Insert(doc);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<ObservedPattern>> QueryAsync(PatternStoreQueryParams query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var db = new LiteDatabase(_connectionString);
        var col = db.GetCollection<PatternDoc>(CollectionName);
        var bsonQuery = col.Query();
        if (query.Since.HasValue)
            bsonQuery = bsonQuery.Where(x => x.LastSeen >= query.Since.Value);
        if (query.Until.HasValue)
            bsonQuery = bsonQuery.Where(x => x.FirstSeen <= query.Until.Value);
        if (!string.IsNullOrWhiteSpace(query.ProjectPath))
            bsonQuery = bsonQuery.Where(x => x.ProjectPath == query.ProjectPath);
        if (!string.IsNullOrWhiteSpace(query.EventType))
            bsonQuery = bsonQuery.Where(x => x.EventType == query.EventType);
        var docs = bsonQuery.OrderByDescending(x => x.LastSeen).Limit(query.MaxCount).ToList();
        var patterns = docs.Select(ToPattern).ToList();
        return Task.FromResult<IReadOnlyList<ObservedPattern>>(patterns);
    }

    /// <inheritdoc/>
    public Task PersistAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    private static void EnsureIndexes(ILiteCollection<PatternDoc> col)
    {
        col.EnsureIndex(x => x.LastSeen);
        col.EnsureIndex(x => x.FirstSeen);
        col.EnsureIndex(x => x.EventType);
        col.EnsureIndex(x => x.ProjectPath);
    }

    private static PatternDoc ToDoc(ObservedPattern p)
    {
        return new PatternDoc
        {
            PatternId = p.PatternId,
            EventType = p.EventType,
            Frequency = p.Frequency,
            FirstSeen = p.FirstSeen,
            LastSeen = p.LastSeen,
            ProjectPath = p.ProjectPath,
            RelatedEventIds = p.RelatedEventIds?.ToArray() ?? Array.Empty<string>(),
            MetadataJson = p.Metadata.HasValue ? p.Metadata.Value.GetRawText() : null,
        };
    }

    private static ObservedPattern ToPattern(PatternDoc d)
    {
        System.Text.Json.JsonElement? metadata = null;
        if (!string.IsNullOrEmpty(d.MetadataJson))
        {
            try
            {
                metadata = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(d.MetadataJson);
            }
            catch
            {
                // Ignore invalid JSON
            }
        }

        return new ObservedPattern
        {
            PatternId = d.PatternId,
            EventType = d.EventType,
            Frequency = d.Frequency,
            FirstSeen = d.FirstSeen,
            LastSeen = d.LastSeen,
            ProjectPath = d.ProjectPath,
            RelatedEventIds = d.RelatedEventIds ?? Array.Empty<string>(),
            Metadata = metadata,
        };
    }

    private sealed class PatternDoc
    {
        /// <summary>Pattern id.</summary>
        [BsonId]
        public string PatternId { get; set; } = string.Empty;
        /// <summary>Event type.</summary>
        public string EventType { get; set; } = string.Empty;
        /// <summary>Frequency.</summary>
        public int Frequency { get; set; }
        /// <summary>First seen.</summary>
        public DateTimeOffset FirstSeen { get; set; }
        /// <summary>Last seen.</summary>
        public DateTimeOffset LastSeen { get; set; }
        /// <summary>Related event ids.</summary>
        public string[]? RelatedEventIds { get; set; }
        /// <summary>Metadata json.</summary>
        public string? MetadataJson { get; set; }
        /// <summary>Path to the generated brick project.</summary>
        public string? ProjectPath { get; set; }
    }
}