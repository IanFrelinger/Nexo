using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Core.Application.Knowledge.Models;
using Ashlar.Core.Application.Knowledge.Ports;
using Ashlar.Core.Application.Observation.Models;
using Ashlar.Core.Application.Observation.Ports;
using Ashlar.Core.Application.Trust.Ports;

namespace Ashlar.Infrastructure.Knowledge;

/// <summary>
/// Aggregates adaptation logs, pattern stores, and user knowledge logs into a unified query result.
/// Tolerates partial backend failures and returns entries from healthy stores only.
/// </summary>
public sealed class KnowledgeQueryService : IKnowledgeQueryService
{
    private readonly IAdaptationLog _adaptationLog;
    private readonly IPatternStore _patternStore;
    private readonly IUserKnowledgeLogStore _knowledgeLogStore;

    /// <summary>Initializes a new knowledge query service.</summary>
    public KnowledgeQueryService(
        IAdaptationLog adaptationLog,
        IPatternStore patternStore,
        IUserKnowledgeLogStore knowledgeLogStore)
    {
        _adaptationLog = adaptationLog ?? throw new ArgumentNullException(nameof(adaptationLog));
        _patternStore = patternStore ?? throw new ArgumentNullException(nameof(patternStore));
        _knowledgeLogStore = knowledgeLogStore ?? throw new ArgumentNullException(nameof(knowledgeLogStore));
    }

    /// <summary>Query asynchronously.</summary>
    public async Task<KnowledgeQueryResult> QueryAsync(KnowledgeQueryRequest request, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeRequest(request);
        var sources = ResolveSources(normalized.Sources);

        var entries = new List<KnowledgeQueryEntry>();
        if (sources.Contains(KnowledgeSource.Adaptation))
        {
            try
            {
                var adaptations = await _adaptationLog
                    .QueryAsync(normalized.Since, normalized.Until, null, cancellationToken)
                    .ConfigureAwait(false);
                entries.AddRange(adaptations.Select(adaptation => new KnowledgeQueryEntry
                {
                    Id = adaptation.Id,
                    Source = KnowledgeSource.Adaptation,
                    Timestamp = adaptation.Timestamp,
                    DataType = adaptation.FailureType,
                    EventType = "adaptation",
                    Summary = $"{adaptation.FailureType}:{adaptation.FilePath ?? "-"} promoted={adaptation.Promoted}",
                    Provenance = BuildProvenance(
                        ("brickId", adaptation.BrickId ?? string.Empty),
                        ("fixApplied", adaptation.FixApplied.ToString()))
                }));
            }
            catch
            {
                // Keep partial results from healthy stores if one backend is unavailable.
            }
        }

        if (sources.Contains(KnowledgeSource.Pattern))
        {
            try
            {
                var patterns = await _patternStore
                    .QueryAsync(new PatternStoreQueryParams
                    {
                        Since = normalized.Since,
                        Until = normalized.Until,
                        EventType = normalized.EventType,
                        MaxCount = normalized.MaxCount + normalized.Offset + 20
                    }, cancellationToken)
                    .ConfigureAwait(false);
                entries.AddRange(patterns.Select(pattern => new KnowledgeQueryEntry
                {
                    Id = pattern.PatternId,
                    Source = KnowledgeSource.Pattern,
                    Timestamp = pattern.LastSeen,
                    DataType = pattern.EventType,
                    EventType = "pattern",
                    Summary = $"frequency={pattern.Frequency} last={pattern.LastSeen:O}",
                    Provenance = BuildPatternProvenance(pattern)
                }));
            }
            catch
            {
                // Keep partial results from healthy stores if one backend is unavailable.
            }
        }

        if (sources.Contains(KnowledgeSource.UserKnowledge))
        {
            try
            {
                var knowledgeEntries = await _knowledgeLogStore
                    .GetAsync(normalized.DataType, normalized.MaxCount + normalized.Offset + 20, cancellationToken)
                    .ConfigureAwait(false);
                entries.AddRange(knowledgeEntries.Select(entry => new KnowledgeQueryEntry
                {
                    Id = entry.Id,
                    Source = KnowledgeSource.UserKnowledge,
                    Timestamp = entry.UpdatedAt,
                    DataType = entry.DataType,
                    EventType = "user-knowledge",
                    Summary = entry.Content,
                    Provenance = BuildKnowledgeProvenance(entry.SourceObservationIds)
                }));
            }
            catch
            {
                // Keep partial results from healthy stores if one backend is unavailable.
            }
        }

        var filtered = entries
            .Where(entry => string.IsNullOrWhiteSpace(normalized.DataType) || string.Equals(entry.DataType, normalized.DataType, StringComparison.OrdinalIgnoreCase))
            .Where(entry => string.IsNullOrWhiteSpace(normalized.EventType) || string.Equals(entry.EventType, normalized.EventType, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(entry => entry.Timestamp)
            .ThenBy(entry => entry.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var total = filtered.Length;
        var page = filtered.Skip(normalized.Offset).Take(normalized.MaxCount).ToArray();
        return new KnowledgeQueryResult
        {
            Entries = page,
            Total = total,
            HasMore = normalized.Offset + page.Length < total
        };
    }

    private static KnowledgeQueryRequest NormalizeRequest(KnowledgeQueryRequest request)
    {
        var maxCount = request.MaxCount <= 0 ? 100 : Math.Min(request.MaxCount, 1000);
        var offset = request.Offset < 0 ? 0 : request.Offset;
        return request with { MaxCount = maxCount, Offset = offset };
    }

    private static HashSet<KnowledgeSource> ResolveSources(IReadOnlyList<KnowledgeSource>? sources)
    {
        if (sources == null || sources.Count == 0)
        {
            return new HashSet<KnowledgeSource>
            {
                KnowledgeSource.Adaptation,
                KnowledgeSource.Pattern,
                KnowledgeSource.UserKnowledge
            };
        }

        return sources.ToHashSet();
    }

    private static IReadOnlyDictionary<string, string> BuildPatternProvenance(ObservedPattern pattern)
    {
        if (pattern.RelatedEventIds == null || pattern.RelatedEventIds.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < pattern.RelatedEventIds.Count; i++)
        {
            map[$"relatedEventId[{i}]"] = pattern.RelatedEventIds[i];
        }

        return map;
    }

    private static IReadOnlyDictionary<string, string> BuildKnowledgeProvenance(IReadOnlyList<string> sourceObservationIds)
    {
        if (sourceObservationIds == null || sourceObservationIds.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < sourceObservationIds.Count; i++)
        {
            map[$"sourceObservationId[{i}]"] = sourceObservationIds[i];
        }

        return map;
    }

    private static IReadOnlyDictionary<string, string> BuildProvenance(params (string Key, string Value)[] pairs)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in pairs)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                map[key] = value;
            }
        }

        return map;
    }
}
