using LiteDB;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;

namespace Nexo.Infrastructure.Adaptation;

/// <summary>
/// LiteDB-backed adaptation audit log.
/// </summary>
public sealed class LiteDbAdaptationAuditLog : IAdaptationAuditLog
{
    private const string CollectionName = "adaptation_audit";
    private readonly string _connectionString;

    /// <summary>Initializes a new lite db adaptation audit log.</summary>
    public LiteDbAdaptationAuditLog(string pathOrConnectionString)
    {
        if (string.IsNullOrWhiteSpace(pathOrConnectionString))
            throw new ArgumentNullException(nameof(pathOrConnectionString));
        var trimmed = pathOrConnectionString.Trim();
        _connectionString = trimmed.StartsWith("Filename=", StringComparison.OrdinalIgnoreCase) ? trimmed : $"Filename={trimmed}";
    }

    /// <inheritdoc />
    public Task LogAsync(AdaptationAuditEntry entry, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var db = new LiteDatabase(_connectionString);
        var col = db.GetCollection<AuditDoc>(CollectionName);
        col.EnsureIndex(x => x.Timestamp);
        col.Insert(ToDoc(entry));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AdaptationAuditEntry>> QueryAsync(DateTimeOffset? since = null, DateTimeOffset? until = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var db = new LiteDatabase(_connectionString);
        var col = db.GetCollection<AuditDoc>(CollectionName);
        var query = col.Query();
        if (since.HasValue)
            query = query.Where(x => x.Timestamp >= since.Value);
        if (until.HasValue)
            query = query.Where(x => x.Timestamp <= until.Value);
        var docs = query.OrderByDescending(x => x.Timestamp).ToList();
        var entries = docs.Select(ToEntry).ToList();
        return Task.FromResult<IReadOnlyList<AdaptationAuditEntry>>(entries);
    }

    private static AuditDoc ToDoc(AdaptationAuditEntry e)
    {
        return new AuditDoc
        {
            Id = e.Id,
            Timestamp = e.Timestamp,
            AutonomyLevel = e.AutonomyLevel,
            Outcome = e.Outcome,
            BrickId = e.BrickId,
            FailureType = e.FailureType,
            FilePath = e.FilePath,
            RegressionPassed = e.RegressionPassed,
            Promoted = e.Promoted,
            Message = e.Message,
        };
    }

    private static AdaptationAuditEntry ToEntry(AuditDoc d)
    {
        return new AdaptationAuditEntry
        {
            Id = d.Id,
            Timestamp = d.Timestamp,
            AutonomyLevel = d.AutonomyLevel,
            Outcome = d.Outcome,
            BrickId = d.BrickId,
            FailureType = d.FailureType,
            FilePath = d.FilePath,
            RegressionPassed = d.RegressionPassed,
            Promoted = d.Promoted,
            Message = d.Message,
        };
    }

    private sealed class AuditDoc
    {
        /// <summary>Id.</summary>
        [BsonId]
        public string Id { get; set; } = string.Empty;
        /// <summary>Timestamp.</summary>
        public DateTimeOffset Timestamp { get; set; }
        /// <summary>Autonomy level.</summary>
        public string AutonomyLevel { get; set; } = string.Empty;
        /// <summary>Outcome.</summary>
        public string Outcome { get; set; } = string.Empty;
        /// <summary>Brick id.</summary>
        public string? BrickId { get; set; }
        /// <summary>Failure type.</summary>
        public string? FailureType { get; set; }
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
