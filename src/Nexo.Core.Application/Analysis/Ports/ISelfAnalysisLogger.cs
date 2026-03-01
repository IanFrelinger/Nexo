using Nexo.Core.Application.Analysis.Models;

namespace Nexo.Core.Application.Analysis.Ports;

/// <summary>
/// Logs Nexo's own adaptation decisions and outcomes (meta-learning).
/// Tracks which adaptations improved things, which made things worse, which had no effect.
/// </summary>
public interface ISelfAnalysisLogger
{
    /// <summary>
    /// Log an adaptation decision and its outcome.
    /// </summary>
    Task LogAsync(SelfAnalysisEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recent entries for context.
    /// </summary>
    Task<IReadOnlyList<SelfAnalysisEntry>> GetRecentAsync(int maxCount = 100, CancellationToken cancellationToken = default);
}
