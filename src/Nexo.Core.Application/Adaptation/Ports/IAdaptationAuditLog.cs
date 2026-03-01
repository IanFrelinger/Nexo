using Nexo.Core.Application.Adaptation.Models;

namespace Nexo.Core.Application.Adaptation.Ports;

/// <summary>
/// Audit log for every adaptation attempt (success or failure) with timestamp, user/autonomy, outcome.
/// </summary>
public interface IAdaptationAuditLog
{
    Task LogAsync(AdaptationAuditEntry entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdaptationAuditEntry>> QueryAsync(DateTimeOffset? since = null, DateTimeOffset? until = null, CancellationToken cancellationToken = default);
}
