using Ashlar.Core.Application.Analysis.Models;

namespace Ashlar.Core.Application.Analysis.Ports;

/// <summary>
/// Logs Ashlar's own adaptation decisions and outcomes (meta-learning).
/// Tracks which adaptations improved things, which made things worse, which had no effect.
/// </summary>
public interface ISelfAnalysisLogger
{
    /// <summary>Logs an adaptation decision and its outcome.</summary>
    /// <param name="entry">Decision entry to persist.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task LogAsync(SelfAnalysisEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Gets recent self-analysis entries for context.</summary>
    /// <param name="maxCount">Maximum number of entries to return.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Recent entries ordered newest first, up to <paramref name="maxCount"/>.</returns>
    Task<IReadOnlyList<SelfAnalysisEntry>> GetRecentAsync(int maxCount = 100, CancellationToken cancellationToken = default);
}
