using LiteDB;
using Nexo.Core.Application.Observation.Ports;

namespace Nexo.Infrastructure.Observation;

/// <summary>
/// LiteDB-backed store for processed pattern IDs. Uses same DB file as pattern store.
/// </summary>
public sealed class LiteDbPatternProcessedStore : IPatternProcessedStore
{
    private const string CollectionName = "processed_patterns";
    private readonly string _connectionString;

    public LiteDbPatternProcessedStore(string pathOrConnectionString)
    {
        if (string.IsNullOrWhiteSpace(pathOrConnectionString))
            throw new ArgumentNullException(nameof(pathOrConnectionString));
        var trimmed = pathOrConnectionString.Trim();
        _connectionString = trimmed.StartsWith("Filename=", StringComparison.OrdinalIgnoreCase) ? trimmed : $"Filename={trimmed}";
    }

    public Task MarkProcessedAsync(string patternId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var db = new LiteDatabase(_connectionString);
        var col = db.GetCollection<ProcessedDoc>(CollectionName);
        col.Insert(new ProcessedDoc { PatternId = patternId, ProcessedAt = DateTimeOffset.UtcNow });
        return Task.CompletedTask;
    }

    public Task<bool> IsProcessedAsync(string patternId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var db = new LiteDatabase(_connectionString);
        var col = db.GetCollection<ProcessedDoc>(CollectionName);
        col.EnsureIndex(x => x.PatternId);
        var doc = col.FindOne(Query.EQ(nameof(ProcessedDoc.PatternId), patternId));
        return Task.FromResult(doc != null);
    }

    private sealed class ProcessedDoc
    {
        [BsonId]
        public ObjectId Id { get; set; } = ObjectId.NewObjectId();
        public string PatternId { get; set; } = string.Empty;
        public DateTimeOffset ProcessedAt { get; set; }
    }
}
