using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Fleet.Models;
using Nexo.Core.Application.Observation.Models;
using Nexo.Core.Application.Observation.Ports;

namespace Nexo.Infrastructure.Fleet;

/// <summary>
/// Applies a <see cref="MeshKnowledgeExportPayload"/> from a peer into local adaptation and pattern stores.
/// Skips duplicates (same adaptation id or pattern id already present).
/// </summary>
public sealed class MeshKnowledgeImportService
{
    private readonly IAdaptationLog _adaptationLog;
    private readonly IPatternStore _patternStore;
    private readonly ILogger<MeshKnowledgeImportService>? _logger;

    public MeshKnowledgeImportService(
        IAdaptationLog adaptationLog,
        IPatternStore patternStore,
        ILogger<MeshKnowledgeImportService>? logger = null)
    {
        _adaptationLog = adaptationLog ?? throw new ArgumentNullException(nameof(adaptationLog));
        _patternStore = patternStore ?? throw new ArgumentNullException(nameof(patternStore));
        _logger = logger;
    }

    public async Task<MeshKnowledgeImportResult> ImportAsync(MeshKnowledgeExportPayload payload, CancellationToken cancellationToken = default)
    {
        var adaptationsApplied = 0;
        var adaptationsSkipped = 0;
        var patternsApplied = 0;
        var patternsSkipped = 0;

        foreach (var record in payload.Adaptations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await _adaptationLog.LogAsync(record, cancellationToken).ConfigureAwait(false);
                adaptationsApplied++;
            }
            catch (Exception ex) when (IsDuplicateInsert(ex))
            {
                adaptationsSkipped++;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "mesh-knowledge import adaptation id={Id} skipped", record.Id);
                adaptationsSkipped++;
            }
        }

        foreach (var pattern in payload.Patterns)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await _patternStore.AddAsync(pattern, cancellationToken).ConfigureAwait(false);
                patternsApplied++;
            }
            catch (Exception ex) when (IsDuplicateInsert(ex))
            {
                patternsSkipped++;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "mesh-knowledge import pattern id={Id} skipped", pattern.PatternId);
                patternsSkipped++;
            }
        }

        return new MeshKnowledgeImportResult(adaptationsApplied, adaptationsSkipped, patternsApplied, patternsSkipped);
    }

    private static bool IsDuplicateInsert(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException)
        {
            if (e.GetType().Name == "LiteException" &&
                e.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}

public sealed record MeshKnowledgeImportResult(
    int AdaptationsApplied,
    int AdaptationsSkipped,
    int PatternsApplied,
    int PatternsSkipped);
