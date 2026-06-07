using System.Text.RegularExpressions;

namespace Nexo.GameDomain.Mapping;

/// <summary>
/// Rule-based verification using parse summaries and inspection notes (no extra network).
/// Suitable for tiling-down sanity checks (layers, tags, feature counts).
/// </summary>
public sealed class HeuristicMapVerificationService : IMapVerificationService
{
    private static readonly Regex GeoJsonFeatureCount =
        new(@"(\d+)\s+feature", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex OsmWaysWithTags =
        new(@"ways-with-tags=(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <inheritdoc />
    public Task<MapVerificationResult> VerifyAsync(
        VectorPayloadInspection inspection,
        VectorMapParseSummary parseSummary,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inspection);
        ArgumentNullException.ThrowIfNull(parseSummary);

        var issues = new List<MapVerificationIssue>();
        var kind = parseSummary.ParserKind;

        if (kind is "error")
        {
            issues.Add(new MapVerificationIssue(
                MapVerificationSeverity.Warning,
                "parse_failed",
                $"Payload classified as failed parse: {parseSummary.Summary}"));
        }

        var blob = string.Join(" ", new[] { parseSummary.Summary }
            .Concat(parseSummary.Details)
            .Concat(inspection.Notes));

        switch (kind)
        {
            case "mvt":
                VerifyMvt(blob, issues);
                break;
            case "geojson":
                VerifyGeoJson(parseSummary.Summary, issues);
                break;
            case "osm_xml":
                VerifyOsmXml(parseSummary.Summary, issues);
                break;
        }

        var passed = issues.All(i => i.Severity < MapVerificationSeverity.Error);
        var summary = BuildSummary(issues);
        return Task.FromResult(new MapVerificationResult(passed, issues, summary));
    }

    private static void VerifyMvt(string blob, List<MapVerificationIssue> issues)
    {
        if (Regex.IsMatch(blob, @"\b(water|ocean|sea|river)\b", RegexOptions.IgnoreCase))
        {
            issues.Add(new MapVerificationIssue(
                MapVerificationSeverity.Info,
                "mvt_water_layer_hint",
                "Layer list or summary suggests water-related features."));
        }

        if (Regex.IsMatch(blob, @"\b(road|street|transport|traffic|highway)\b", RegexOptions.IgnoreCase))
        {
            issues.Add(new MapVerificationIssue(
                MapVerificationSeverity.Info,
                "mvt_transport_layer_hint",
                "Layer list or summary suggests transportation-related features."));
        }
        else if (blob.Contains("layers:", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new MapVerificationIssue(
                MapVerificationSeverity.Info,
                "mvt_sparse_tile",
                "No obvious transport keywords in layer summary (tile may be rural or sparse)."));
        }
    }

    private static void VerifyGeoJson(string summary, List<MapVerificationIssue> issues)
    {
        var m = GeoJsonFeatureCount.Match(summary);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var n))
        {
            if (n == 0)
            {
                issues.Add(new MapVerificationIssue(
                    MapVerificationSeverity.Warning,
                    "geojson_zero_features",
                    "GeoJSON summary reports zero features."));
            }
            else
            {
                issues.Add(new MapVerificationIssue(
                    MapVerificationSeverity.Info,
                    "geojson_feature_count",
                    $"Counted {n} feature(s) in summary."));
            }
        }
    }

    private static void VerifyOsmXml(string summary, List<MapVerificationIssue> issues)
    {
        var m = OsmWaysWithTags.Match(summary);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var ways))
        {
            if (ways == 0)
                issues.Add(new MapVerificationIssue(
                    MapVerificationSeverity.Warning,
                    "osm_no_tagged_ways",
                    "No ways with tags reported — extract may be nodes-only or empty."));
            else
                issues.Add(new MapVerificationIssue(
                    MapVerificationSeverity.Info,
                    "osm_tagged_ways",
                    $"{ways} way(s) with tags."));
        }

        if (summary.Contains("highway", StringComparison.OrdinalIgnoreCase) ||
            summary.Contains("waterway", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new MapVerificationIssue(
                MapVerificationSeverity.Info,
                "osm_corridor_tags",
                "Top tag keys include highway/waterway-related hints."));
        }
    }

    private static string BuildSummary(IReadOnlyList<MapVerificationIssue> issues)
    {
        if (issues.Count == 0)
            return "no issues";

        static char Rank(MapVerificationSeverity s) => s switch
        {
            MapVerificationSeverity.Error => 'E',
            MapVerificationSeverity.Warning => 'W',
            _ => 'I'
        };

        var top = issues
            .OrderByDescending(i => (int)i.Severity)
            .Take(4)
            .Select(i => $"{Rank(i.Severity)}:{i.Code}");
        return string.Join("; ", top) + (issues.Count > 4 ? "…" : string.Empty);
    }
}
