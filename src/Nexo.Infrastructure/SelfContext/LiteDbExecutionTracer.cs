using System.Text.Json;
using LiteDB;
using Nexo.Core.Application.SelfContext.Models;
using Nexo.Core.Application.SelfContext.Ports;

namespace Nexo.Infrastructure.SelfContext;

/// <summary>
/// LiteDB-backed execution tracer.
/// </summary>
public sealed class LiteDbExecutionTracer : IExecutionTracer
{
    private const string CollectionName = "execution_traces";
    private readonly string _connectionString;

    /// <summary>Initializes a new lite db execution tracer.</summary>
    public LiteDbExecutionTracer(string pathOrConnectionString)
    {
        if (string.IsNullOrWhiteSpace(pathOrConnectionString))
            throw new ArgumentNullException(nameof(pathOrConnectionString));
        var trimmed = pathOrConnectionString.Trim();
        _connectionString = trimmed.StartsWith("Filename=", StringComparison.OrdinalIgnoreCase) ? trimmed : $"Filename={trimmed}";
    }

    /// <inheritdoc />
    public Task TraceAsync(string operation, IReadOnlyDictionary<string, object>? context = null, string? path = null, string? outcome = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var entry = new ExecutionTraceEntry
        {
            Id = Guid.NewGuid().ToString("N"),
            Timestamp = DateTimeOffset.UtcNow,
            Operation = operation,
            Path = path,
            Outcome = outcome,
            Context = context,
        };
        return LogAsync(entry, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ExecutionTraceEntry>> QueryAsync(DateTimeOffset? since = null, DateTimeOffset? until = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var db = new LiteDatabase(_connectionString);
        var col = db.GetCollection<TraceDoc>(CollectionName);
        var query = col.Query();
        if (since.HasValue)
            query = query.Where(x => x.Timestamp >= since.Value);
        if (until.HasValue)
            query = query.Where(x => x.Timestamp <= until.Value);
        var docs = query.OrderByDescending(x => x.Timestamp).Limit(500).ToList();
        var entries = docs.Select(ToEntry).ToList();
        return Task.FromResult<IReadOnlyList<ExecutionTraceEntry>>(entries);
    }

    private Task LogAsync(ExecutionTraceEntry entry, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var db = new LiteDatabase(_connectionString);
        var col = db.GetCollection<TraceDoc>(CollectionName);
        col.EnsureIndex(x => x.Timestamp);
        col.Insert(ToDoc(entry));
        return Task.CompletedTask;
    }

    private static TraceDoc ToDoc(ExecutionTraceEntry e)
    {
        string? contextJson = null;
        if (e.Context != null && e.Context.Count > 0)
        {
            try
            {
                var dict = e.Context.ToDictionary(kv => kv.Key, kv => kv.Value ?? "");
                contextJson = System.Text.Json.JsonSerializer.Serialize(dict);
            }
            catch
            {
                // Skip context if serialization fails
            }
        }
        return new TraceDoc
        {
            Id = e.Id,
            Timestamp = e.Timestamp,
            Operation = e.Operation,
            Path = e.Path,
            Outcome = e.Outcome,
            ContextJson = contextJson,
        };
    }

    private static ExecutionTraceEntry ToEntry(TraceDoc d)
    {
        IReadOnlyDictionary<string, object>? context = null;
        if (!string.IsNullOrEmpty(d.ContextJson))
        {
            try
            {
                var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(d.ContextJson);
                if (dict != null)
                    context = new Dictionary<string, object>(dict.ToDictionary(kv => kv.Key, kv => (object)kv.Value));
            }
            catch
            {
                // Ignore invalid JSON
            }
        }
        return new ExecutionTraceEntry
        {
            Id = d.Id,
            Timestamp = d.Timestamp,
            Operation = d.Operation,
            Path = d.Path,
            Outcome = d.Outcome,
            Context = context,
        };
    }

    private sealed class TraceDoc
    {
        /// <summary>Id.</summary>
        [BsonId]
        public string Id { get; set; } = string.Empty;
        /// <summary>Timestamp.</summary>
        public DateTimeOffset Timestamp { get; set; }
        /// <summary>Operation.</summary>
        public string Operation { get; set; } = string.Empty;
        /// <summary>Path.</summary>
        public string? Path { get; set; }
        /// <summary>Outcome.</summary>
        public string? Outcome { get; set; }
        /// <summary>Context json.</summary>
        public string? ContextJson { get; set; }
    }
}
