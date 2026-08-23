using LiteDB;
using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Adaptation.Ports;

namespace Ashlar.Infrastructure.Adaptation;

/// <summary>
/// LiteDB-backed adaptation log. Persistent, queryable.
/// </summary>
public sealed class LiteDbAdaptationLog : IAdaptationLog
{
    private const string CollectionName = "adaptation_records";
    private readonly string _connectionString;

    /// <summary>Initializes a new lite db adaptation log.</summary>
    public LiteDbAdaptationLog(string pathOrConnectionString)
    {
        if (string.IsNullOrWhiteSpace(pathOrConnectionString))
            throw new ArgumentNullException(nameof(pathOrConnectionString));
        var trimmed = pathOrConnectionString.Trim();
        _connectionString = trimmed.StartsWith("Filename=", StringComparison.OrdinalIgnoreCase) ? trimmed : $"Filename={trimmed}";
    }

    /// <inheritdoc />
    public Task LogAsync(AdaptationRecord record, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var db = new LiteDatabase(_connectionString);
        var col = db.GetCollection<AdaptationDoc>(CollectionName);
        EnsureIndexes(col);
        col.Insert(ToDoc(record));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AdaptationRecord>> QueryAsync(DateTimeOffset? since = null, DateTimeOffset? until = null, string? brickId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var db = new LiteDatabase(_connectionString);
        var col = db.GetCollection<AdaptationDoc>(CollectionName);
        var query = col.Query();
        if (since.HasValue)
            query = query.Where(x => x.Timestamp >= since.Value);
        if (until.HasValue)
            query = query.Where(x => x.Timestamp <= until.Value);
        if (!string.IsNullOrWhiteSpace(brickId))
            query = query.Where(x => x.BrickId == brickId);
        var docs = query.OrderByDescending(x => x.Timestamp).ToList();
        var records = docs.Select(ToRecord).ToList();
        return Task.FromResult<IReadOnlyList<AdaptationRecord>>(records);
    }

    private static void EnsureIndexes(ILiteCollection<AdaptationDoc> col)
    {
        col.EnsureIndex(x => x.Timestamp);
        col.EnsureIndex(x => x.BrickId);
    }

    private static AdaptationDoc ToDoc(AdaptationRecord r)
    {
        return new AdaptationDoc
        {
            Id = r.Id,
            Timestamp = r.Timestamp,
            BrickId = r.BrickId,
            FailureType = r.FailureType,
            FixApplied = (int)r.FixApplied,
            FilePath = r.FilePath,
            RegressionPassed = r.RegressionPassed,
            Promoted = r.Promoted,
            Message = r.Message,
        };
    }

    private static AdaptationRecord ToRecord(AdaptationDoc d)
    {
        return new AdaptationRecord
        {
            Id = d.Id,
            Timestamp = d.Timestamp,
            BrickId = d.BrickId,
            FailureType = d.FailureType,
            FixApplied = (AdaptationFixType)d.FixApplied,
            FilePath = d.FilePath,
            RegressionPassed = d.RegressionPassed,
            Promoted = d.Promoted,
            Message = d.Message,
        };
    }

    private sealed class AdaptationDoc
    {
        /// <summary>Id.</summary>
        [BsonId]
        public string Id { get; set; } = string.Empty;
        /// <summary>Timestamp.</summary>
        public DateTimeOffset Timestamp { get; set; }
        /// <summary>Brick id.</summary>
        public string? BrickId { get; set; }
        /// <summary>Failure type.</summary>
        public string FailureType { get; set; } = string.Empty;
        /// <summary>Fix applied.</summary>
        public int FixApplied { get; set; }
        /// <summary>File path.</summary>
        public string? FilePath { get; set; }
        /// <summary>Regression passed.</summary>
        public bool RegressionPassed { get; set; }
        /// <summary>Promoted.</summary>
        public bool Promoted { get; set; }
        /// <summary>Message.</summary>
        public string? Message { get; set; }
    }
}
