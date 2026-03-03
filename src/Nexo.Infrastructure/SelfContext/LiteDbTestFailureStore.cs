using LiteDB;
using Nexo.Core.Application.SelfContext.Models;
using Nexo.Core.Application.SelfContext.Ports;

namespace Nexo.Infrastructure.SelfContext;

/// <summary>
/// LiteDB-backed test failure store. Phase F: wire test failures into adaptation trigger.
/// </summary>
public sealed class LiteDbTestFailureStore : ITestFailureStore
{
    private const string CollectionName = "test_failures";
    private readonly string _connectionString;

    public LiteDbTestFailureStore(string pathOrConnectionString)
    {
        if (string.IsNullOrWhiteSpace(pathOrConnectionString))
            throw new ArgumentNullException(nameof(pathOrConnectionString));
        var trimmed = pathOrConnectionString.Trim();
        _connectionString = trimmed.StartsWith("Filename=", StringComparison.OrdinalIgnoreCase) ? trimmed : $"Filename={trimmed}";
    }

    /// <inheritdoc />
    public Task RecordAsync(TestFailureRecord record, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var db = new LiteDatabase(_connectionString);
        var col = db.GetCollection<TestFailureDoc>(CollectionName);
        col.EnsureIndex(x => x.Timestamp);
        col.Insert(new TestFailureDoc
        {
            Id = record.Id,
            Timestamp = record.Timestamp,
            TestName = record.TestName,
            FilePath = record.FilePath,
            ErrorMessage = record.ErrorMessage,
            StackTrace = record.StackTrace,
        });
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TestFailureRecord>> QueryAsync(DateTimeOffset? since = null, DateTimeOffset? until = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var db = new LiteDatabase(_connectionString);
        var col = db.GetCollection<TestFailureDoc>(CollectionName);
        var query = col.Query();
        if (since.HasValue)
            query = query.Where(x => x.Timestamp >= since.Value);
        if (until.HasValue)
            query = query.Where(x => x.Timestamp <= until.Value);
        var docs = query.OrderByDescending(x => x.Timestamp).Limit(100).ToList();
        var records = docs.Select(d => new TestFailureRecord
        {
            Id = d.Id,
            Timestamp = d.Timestamp,
            TestName = d.TestName,
            FilePath = d.FilePath,
            ErrorMessage = d.ErrorMessage,
            StackTrace = d.StackTrace,
        }).ToList();
        return Task.FromResult<IReadOnlyList<TestFailureRecord>>(records);
    }

    private sealed class TestFailureDoc
    {
        [BsonId]
        public string Id { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public string? ErrorMessage { get; set; }
        public string? StackTrace { get; set; }
    }
}
