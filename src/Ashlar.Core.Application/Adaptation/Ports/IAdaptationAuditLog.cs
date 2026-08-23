using Ashlar.Core.Application.Adaptation.Models;

namespace Ashlar.Core.Application.Adaptation.Ports;

/// <summary>
/// Audit log for every adaptation attempt (success or failure) with timestamp, user/autonomy, outcome.
/// </summary>
public interface IAdaptationAuditLog
{
    /// <summary>Persists an audit entry for an adaptation attempt.</summary>
    /// <param name="entry">Audit entry with autonomy level and outcome.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task LogAsync(AdaptationAuditEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Queries audit entries within an optional time range.</summary>
    /// <param name="since">Earliest timestamp to include.</param>
    /// <param name="until">Latest timestamp to include.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<IReadOnlyList<AdaptationAuditEntry>> QueryAsync(DateTimeOffset? since = null, DateTimeOffset? until = null, CancellationToken cancellationToken = default);
}
