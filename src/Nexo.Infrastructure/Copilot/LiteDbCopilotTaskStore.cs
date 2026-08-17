using LiteDB;
using Nexo.Core.Application.Copilot.Models;
using Nexo.Core.Application.Copilot.Ports;

namespace Nexo.Infrastructure.Copilot;

/// <summary>
/// LiteDB-backed store for copilot task history (API correlation / audit).
/// </summary>
public sealed class LiteDbCopilotTaskStore : ICopilotTaskStore
{
    private const string CollectionName = "copilot_tasks";
    private readonly string _connectionString;

    /// <summary>Initializes a new lite db copilot task store.</summary>
    public LiteDbCopilotTaskStore(string pathOrConnectionString)
    {
        if (string.IsNullOrWhiteSpace(pathOrConnectionString))
            throw new ArgumentNullException(nameof(pathOrConnectionString));
        var trimmed = pathOrConnectionString.Trim();
        var withFilename = trimmed.StartsWith("Filename=", StringComparison.OrdinalIgnoreCase) ? trimmed : $"Filename={trimmed}";

        // Shared, not LiteDB's default Direct. Direct takes an EXCLUSIVE file lock for the lifetime of
        // the LiteDatabase instance, and every method here opens one per call, so two concurrent
        // submissions raced: the second threw IOException ("used by another process") out of an API
        // request that had already RUN the task. The work happened and its record was lost, which is
        // the one failure the audit trail cannot absorb. Shared mode serialises through a named mutex
        // instead — it also covers a second process (the CLI) touching the same file.
        // An explicit Connection= supplied by the caller is left alone.
        _connectionString = withFilename.Contains("Connection=", StringComparison.OrdinalIgnoreCase)
            ? withFilename
            : $"{withFilename};Connection=Shared";
    }

    /// <inheritdoc />
    public Task<CopilotTaskRecord> StoreAsync(CopilotTaskRecord record, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        using var db = new LiteDatabase(_connectionString);
        var col = db.GetCollection<CopilotTaskDoc>(CollectionName);
        col.EnsureIndex(x => x.SubmittedAt);
        col.EnsureIndex(x => x.TenantId);
        col.Upsert(ToDoc(record));
        return Task.FromResult(record);
    }

    /// <inheritdoc />
    public Task<CopilotTaskRecord?> GetByIdAsync(string taskId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(taskId))
            return Task.FromResult<CopilotTaskRecord?>(null);

        using var db = new LiteDatabase(_connectionString);
        var col = db.GetCollection<CopilotTaskDoc>(CollectionName);
        var doc = col.FindById(taskId.Trim());
        return Task.FromResult(doc is null ? null : ToRecord(doc));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CopilotTaskRecord>> QueryAsync(int maxCount = 50, DateTimeOffset? since = null, string tenantId = "default", CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var limit = maxCount <= 0 ? 50 : Math.Min(maxCount, 500);
        var tid = NormalizeTenantId(tenantId);
        using var db = new LiteDatabase(_connectionString);
        var col = db.GetCollection<CopilotTaskDoc>(CollectionName);
        ILiteQueryable<CopilotTaskDoc> query = col.Query();
        query = tid == "default"
            ? query.Where(x => x.TenantId == "default" || x.TenantId == null || x.TenantId == "")
            : query.Where(x => x.TenantId == tid);
        if (since.HasValue)
            query = query.Where(x => x.SubmittedAt >= since.Value);
        var docs = query.OrderByDescending(x => x.SubmittedAt).Limit(limit).ToList();
        var records = docs.Select(ToRecord).ToList();
        return Task.FromResult<IReadOnlyList<CopilotTaskRecord>>(records);
    }

    private static string NormalizeTenantId(string tenantId)
    {
        var t = string.IsNullOrWhiteSpace(tenantId) ? "default" : tenantId.Trim();
        return t.Length > 128 ? t[..128] : t;
    }

    private static CopilotTaskDoc ToDoc(CopilotTaskRecord r) => new()
    {
        TenantId = NormalizeTenantId(r.TenantId),
        TaskId = r.TaskId,
        Task = r.Task,
        SubmittedAt = r.SubmittedAt,
        CompletedAt = r.CompletedAt,
        Success = r.Success,
        Summary = r.Summary,
        Error = r.Error
    };

    private static CopilotTaskRecord ToRecord(CopilotTaskDoc d) => new()
    {
        TenantId = string.IsNullOrWhiteSpace(d.TenantId) ? "default" : NormalizeTenantId(d.TenantId),
        TaskId = d.TaskId,
        Task = d.Task,
        SubmittedAt = d.SubmittedAt,
        CompletedAt = d.CompletedAt,
        Success = d.Success,
        Summary = d.Summary,
        Error = d.Error
    };

    private sealed class CopilotTaskDoc
    {
        /// <summary>Tenant id.</summary>
        public string TenantId { get; set; } = "default";

        /// <summary>Task id.</summary>
        [BsonId]
        public string TaskId { get; set; } = string.Empty;
        /// <summary>Task.</summary>
        public string Task { get; set; } = string.Empty;
        /// <summary>Submitted at.</summary>
        public DateTimeOffset SubmittedAt { get; set; }
        /// <summary>Completed at.</summary>
        public DateTimeOffset? CompletedAt { get; set; }
        /// <summary>Whether execution completed successfully.</summary>
        public bool Success { get; set; }
        /// <summary>Summary.</summary>
        public string? Summary { get; set; }
        /// <summary>Error.</summary>
        public string? Error { get; set; }
    }
}
