using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Commercial.Fleet.Contracts.Models;
using Ashlar.Core.Application.Observation.Models;
using Ashlar.Core.Application.Observation.Ports;

namespace Ashlar.Commercial.Fleet.Infrastructure;

/// <summary>
/// Builds a <see cref="MeshKnowledgeExportPayload"/> from local adaptation and pattern stores (Phase 4 hub export).
/// </summary>
public sealed class MeshKnowledgeExportService
{
    private readonly IAdaptationLog _adaptationLog;
    private readonly IPatternStore _patternStore;

    public MeshKnowledgeExportService(IAdaptationLog adaptationLog, IPatternStore patternStore)
    {
        _adaptationLog = adaptationLog ?? throw new ArgumentNullException(nameof(adaptationLog));
        _patternStore = patternStore ?? throw new ArgumentNullException(nameof(patternStore));
    }

    /// <summary>Export async operation.</summary>
    public async Task<MeshKnowledgeExportPayload> ExportAsync(
        DateTimeOffset? since,
        int maxAdaptations,
        int maxPatterns,
        CancellationToken cancellationToken = default)
    {
        maxAdaptations = Math.Clamp(maxAdaptations, 1, 10_000);
        maxPatterns = Math.Clamp(maxPatterns, 1, 10_000);

        var adaptations = await _adaptationLog
            .QueryAsync(since, until: null, brickId: null, cancellationToken)
            .ConfigureAwait(false);
        var adaptationList = adaptations
            .OrderByDescending(a => a.Timestamp)
            .Take(maxAdaptations)
            .ToList();

        var patterns = await _patternStore
            .QueryAsync(new PatternStoreQueryParams
            {
                Since = since,
                MaxCount = maxPatterns
            }, cancellationToken)
            .ConfigureAwait(false);

        return new MeshKnowledgeExportPayload(
            ExportedAt: DateTimeOffset.UtcNow,
            Adaptations: adaptationList,
            Patterns: patterns.ToList());
    }
}
