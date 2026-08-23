using Ashlar.Core.Application.Copilot.Models;

namespace Ashlar.Core.Application.Copilot.Ports;

/// <summary>
/// Persistence port for copilot task records (LiteDB or other backing store).
/// </summary>
public interface ICopilotTaskStore
{
    /// <summary>Persists or updates a copilot task record.</summary>
    Task<CopilotTaskRecord> StoreAsync(CopilotTaskRecord record, CancellationToken ct = default);

    /// <summary>Loads a task by id, or null when not found.</summary>
    Task<CopilotTaskRecord?> GetByIdAsync(string taskId, CancellationToken ct = default);

    /// <summary>Queries recent tasks for a tenant, optionally filtered by time.</summary>
    Task<IReadOnlyList<CopilotTaskRecord>> QueryAsync(int maxCount = 50, DateTimeOffset? since = null, string tenantId = "default", CancellationToken ct = default);
}
