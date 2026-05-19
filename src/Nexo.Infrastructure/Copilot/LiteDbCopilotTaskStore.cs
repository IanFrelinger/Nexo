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

    public LiteDbCopilotTaskStore(string pathOrConnectionString)
    {
        if (string.IsNullOrWhiteSpace(pathOrConnectionString))
            throw new ArgumentNullException(nameof(pathOrConnectionString));
        var trimmed = pathOrConnectionString.Trim();
        _connectionString = trimmed.StartsWith("Filename=", StringComparison.OrdinalIgnoreCase) ? trimmed : $"Filename={trimmed}";
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
        public string TenantId { get; set; } = "default";

        [BsonId]
        public string TaskId { get; set; } = string.Empty;
        public string Task { get; set; } = string.Empty;
        public DateTimeOffset SubmittedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public bool Success { get; set; }
        public string? Summary { get; set; }
        public string? Error { get; set; }
    }
}
