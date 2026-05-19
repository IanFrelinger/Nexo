using Nexo.Core.Application.Copilot.Models;

namespace Nexo.Core.Application.Copilot.Ports;

public interface ICopilotTaskStore
{
    Task<CopilotTaskRecord> StoreAsync(CopilotTaskRecord record, CancellationToken ct = default);
    Task<CopilotTaskRecord?> GetByIdAsync(string taskId, CancellationToken ct = default);
    Task<IReadOnlyList<CopilotTaskRecord>> QueryAsync(int maxCount = 50, DateTimeOffset? since = null, string tenantId = "default", CancellationToken ct = default);
}
