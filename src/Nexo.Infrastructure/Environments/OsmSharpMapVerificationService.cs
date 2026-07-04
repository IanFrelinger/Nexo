using System.Globalization;
using Nexo.Core.Application.Environments;
using Nexo.Core.Application.Environments.Ports;
using OsmSharp.Complete;
using OsmSharp.Streams;
using OsmSharp.Streams.Complete;

namespace Nexo.Infrastructure.Environments;

/// <summary>
/// Parses OSM XML samples with OsmSharp and emits deterministic topology checks for water, roads,
/// and polygon closure. Does not fetch remote data — uses <see cref="MapVerificationRequest.VectorSamplePayload"/>.
/// When vector sample payload is absent, emits an info issue only (does not fail core checks).
/// </summary>
public sealed class OsmSharpMapVerificationService : IMapVerificationService
{
    /// <summary>Verify asynchronously.</summary>
    public Task<MapVerificationReport> VerifyAsync(
        MapVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var issues = new List<MapVerificationIssue>();

        if (request.ParentTierReport is { Issues.Count: > 0 } parent)
        {
            foreach (var p in parent.Issues.Where(i =>
                         string.Equals(i.Severity, MapVerificationSeverity.Error, StringComparison.OrdinalIgnoreCase)))
            {
                issues.Add(new MapVerificationIssue(
                    p.Category,
                    MapVerificationSeverity.Warning,
                    "Parent tier reported error — verify refinement at this tier: " + p.Message,
                    MergeEvidence(p.Evidence, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["parent_tier"] = parent.TierIndex.ToString(CultureInfo.InvariantCulture),
                        ["current_tier"] = request.TierIndex.ToString(CultureInfo.InvariantCulture)
                    })));
            }
        }

        var payload = request.VectorSamplePayload;
        if (!payload.HasValue || payload.Value.IsEmpty)
        {
            issues.Add(new MapVerificationIssue(
                MapVerificationCategories.Topology,
                MapVerificationSeverity.Info,
                "No vector sample payload — skipping OsmSharp geometry checks",
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["tier"] = request.TierIndex.ToString(CultureInfo.InvariantCulture)
                }));
            return Task.FromResult(new MapVerificationReport(request.TierIndex, issues,
                PassedCoreChecks: !issues.Any(i =>
                    string.Equals(i.Severity, MapVerificationSeverity.Error, StringComparison.OrdinalIgnoreCase))));
        }

        var hint = request.VectorFormatHint ?? "";
        if (!hint.Contains("osm", StringComparison.OrdinalIgnoreCase) &&
            !LooksLikeXml(payload.Value.Span))
        {
            issues.Add(new MapVerificationIssue(
                MapVerificationCategories.Topology,
                MapVerificationSeverity.Info,
                "Vector format hint is not OSM XML — OsmSharp checks skipped",
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["hint"] = hint }));
            return Task.FromResult(Report(request.TierIndex, issues));
        }

        try
        {
            using var ms = new MemoryStream(payload.Value.ToArray(), writable: false);
            var xml = new OsmSharp.Streams.XmlOsmStreamSource(ms);
            var complete = new OsmSimpleCompleteStreamSource(xml);

            var ways = new List<CompleteWay>();
            foreach (var g in complete)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (g is CompleteWay cw)
                    ways.Add(cw);
            }

            foreach (var w in ways)
                AnalyzeWay(w, issues, request.TierIndex);
        }
        catch (Exception ex)
        {
            issues.Add(new MapVerificationIssue(
                MapVerificationCategories.Topology,
                MapVerificationSeverity.Warning,
                "OSM parse failed: " + ex.Message,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["tier"] = request.TierIndex.ToString(CultureInfo.InvariantCulture)
                }));
        }

        return Task.FromResult(Report(request.TierIndex, issues));
    }

    private static MapVerificationReport Report(int tier, List<MapVerificationIssue> issues) =>
        new(tier, issues,
            PassedCoreChecks: !issues.Any(i =>
                string.Equals(i.Severity, MapVerificationSeverity.Error, StringComparison.OrdinalIgnoreCase)));

    private static void AnalyzeWay(CompleteWay way, List<MapVerificationIssue> issues, int tier)
    {
        var tags = way.Tags;
        var highway = GetTag(tags, "highway");
        var waterway = GetTag(tags, "waterway");
        var natural = GetTag(tags, "natural");
        var building = GetTag(tags, "building");

        var id = way.Id.ToString(CultureInfo.InvariantCulture);
        var tierStr = tier.ToString(CultureInfo.InvariantCulture);
        var baseEv = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["way_id"] = id,
            ["tier"] = tierStr
        };

        var nodes = way.Nodes;
        if (nodes.Length < 2)
        {
            issues.Add(new MapVerificationIssue(
                MapVerificationCategories.Topology,
                MapVerificationSeverity.Warning,
                $"Way {id} has fewer than 2 positioned nodes",
                baseEv));
            return;
        }

        var closed = nodes.Length >= 4 && nodes[0].Id == nodes[^1].Id;

        var wantsWaterPolygon = natural is "water" or "bay" || waterway is "riverbank";
        var isRoadLike = highway is not null;
        var hasBuildingTag = building is not null;

        if (wantsWaterPolygon && !closed)
        {
            issues.Add(new MapVerificationIssue(
                MapVerificationCategories.Water,
                MapVerificationSeverity.Warning,
                $"Water-related way {id} should be a closed polygon but first/last node do not match",
                baseEv));
        }

        if (isRoadLike && nodes.Length == 2 && highway is "primary" or "secondary" or "tertiary" or "trunk" or "motorway")
        {
            issues.Add(new MapVerificationIssue(
                MapVerificationCategories.Road,
                MapVerificationSeverity.Info,
                $"Road way {id} is a single segment — check connectivity at tier {tier}",
                baseEv));
        }

        if (hasBuildingTag && !closed && nodes.Length >= 3)
        {
            issues.Add(new MapVerificationIssue(
                MapVerificationCategories.Building,
                MapVerificationSeverity.Warning,
                $"Building way {id} is not closed — footprint may be incomplete",
                baseEv));
        }

        if (wantsWaterPolygon && closed && nodes.Length < 4)
        {
            issues.Add(new MapVerificationIssue(
                MapVerificationCategories.Water,
                MapVerificationSeverity.Warning,
                $"Water polygon {id} has fewer than four vertices",
                baseEv));
        }
    }

    private static IReadOnlyDictionary<string, string>? MergeEvidence(
        IReadOnlyDictionary<string, string>? a,
        Dictionary<string, string> b)
    {
        if (a is null || a.Count == 0)
            return b;
        var merged = new Dictionary<string, string>(a, StringComparer.OrdinalIgnoreCase);
        foreach (var kv in b)
            merged[kv.Key] = kv.Value;
        return merged;
    }

    private static bool LooksLikeXml(ReadOnlySpan<byte> span)
    {
        var i = 0;
        while (i < span.Length && char.IsWhiteSpace((char)span[i]))
            i++;
        return i < span.Length && span[i] == (byte)'<';
    }

    private static string? GetTag(OsmSharp.Tags.TagsCollectionBase tags, string key) =>
        tags.TryGetValue(key, out var v) ? v : null;
}
