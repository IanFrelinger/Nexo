using System.Text.Json;
using System.Text.Json.Serialization;
using Nexo.Spike.S1.Adversary;
using Nexo.Spike.S1.Transforms;

namespace Nexo.Spike.S1.Reporting;

public enum CandidateOutcomeKind
{
    Escape,
    Caught,
    Accepted,
    FalseReject,
    Blocked
}

public sealed record CandidateOutcome(
    TransformTag Tag,
    TransformFamily Family,
    int Seed,
    CandidateOutcomeKind Kind,
    string? Detail,
    string Hypothesis,
    string? CaughtBy,
    string? MissingRelation,
    string? DiffSummary);

public sealed record TransformBreakdown(
    int Total,
    int Escapes,
    int Caught,
    int FalseRejects,
    int Blocked,
    double EscapeRate,
    double FalseRejectRate);

public sealed record DimensionReport(
    string Status,
    string Gate,
    int TotalCandidates,
    int Escapes,
    int Caught,
    int FalseRejects,
    int Blocked,
    double EscapeRate,
    double FalseRejectRate,
    IReadOnlyDictionary<string, TransformBreakdown> PerTransform);

public sealed record ToolAvailability(
    bool DotnetAvailable,
    bool StrykerAvailable,
    string? StrykerDetail);

public sealed record ThresholdSweepPoint(
    double ThresholdPercent,
    int Escapes,
    int Caught,
    double EscapeRate);

public sealed record ThresholdSensitivityReport(
    IReadOnlyList<double> Thresholds,
    IReadOnlyDictionary<string, IReadOnlyList<ThresholdSweepPoint>> PerTransform,
    IReadOnlyDictionary<string, double?> FirstEscapeThreshold);

public sealed record SurvivingExample(
    string Dimension,
    string TransformTag,
    int Seed,
    string WorkspacePath,
    string Hypothesis,
    string MissingRelation,
    string DiffSummary);

public sealed record EscapeRateReport(
    string ReportVersion,
    string CatalogVersion,
    string AdversaryMode,
    int Seeds,
    int MutationSample,
    int BudgetMinutes,
    int DistinctWrongImplTrials,
    int DistinctWeakTestTrials,
    int TotalWrongImplCandidates,
    int TotalWeakTestCandidates,
    ToolAvailability Tools,
    DimensionReport WrongImpl,
    DimensionReport WeakTest,
    ThresholdSensitivityReport? ThresholdSensitivity,
    IReadOnlyList<CandidateOutcome> Attributions,
    IReadOnlyList<SurvivingExample> SurvivingExamples)
{
    public const string Version = "s1.4-v1";
}

public static class EscapeRateTally
{
    public static TransformBreakdown TallyTransform(
        IReadOnlyList<CandidateOutcome> outcomes,
        TransformTag tag)
    {
        var subset = outcomes.Where(o => o.Tag == tag).ToList();
        return Tally(subset);
    }

    public static TransformBreakdown Tally(IReadOnlyList<CandidateOutcome> outcomes)
    {
        var total = outcomes.Count;
        var escapes = outcomes.Count(o => o.Kind == CandidateOutcomeKind.Escape);
        var caught = outcomes.Count(o => o.Kind == CandidateOutcomeKind.Caught);
        var accepted = outcomes.Count(o => o.Kind == CandidateOutcomeKind.Accepted);
        var falseRejects = outcomes.Count(o => o.Kind == CandidateOutcomeKind.FalseReject);
        var blocked = outcomes.Count(o => o.Kind == CandidateOutcomeKind.Blocked);
        var adversarial = escapes + caught;
        var baseline = accepted + falseRejects;

        return new TransformBreakdown(
            total,
            escapes,
            caught,
            falseRejects,
            blocked,
            adversarial == 0 ? 0 : (double)escapes / adversarial,
            baseline == 0 ? 0 : (double)falseRejects / baseline);
    }

    public static DimensionReport BuildDimensionReport(
        string gate,
        string status,
        IReadOnlyList<CandidateOutcome> adversarialOutcomes,
        IReadOnlyList<CandidateOutcome> baselineOutcomes,
        IReadOnlyList<TransformTag> adversarialTags)
    {
        var adversarialTally = Tally(adversarialOutcomes);
        var baselineTally = Tally(baselineOutcomes);
        var combined = adversarialOutcomes.Concat(baselineOutcomes).ToList();
        var perTransform = adversarialTags
            .Concat([TransformTag.HonestNoOp])
            .Distinct()
            .ToDictionary(
                tag => tag.ToString(),
                tag => TallyTransform(combined, tag));

        return new DimensionReport(
            status,
            gate,
            combined.Count,
            adversarialTally.Escapes,
            adversarialTally.Caught,
            baselineTally.FalseRejects,
            adversarialTally.Blocked + baselineTally.Blocked,
            adversarialTally.EscapeRate,
            baselineTally.FalseRejectRate,
            perTransform);
    }

    public static ThresholdSensitivityReport BuildThresholdSensitivity(
        IReadOnlyList<double> thresholds,
        IReadOnlyDictionary<TransformTag, IReadOnlyList<CandidateOutcomeKind>> sweepResults)
    {
        var perTransform = new Dictionary<string, IReadOnlyList<ThresholdSweepPoint>>();
        var firstEscape = new Dictionary<string, double?>();

        foreach (var (tag, results) in sweepResults)
        {
            var points = new List<ThresholdSweepPoint>();
            double? first = null;
            for (var i = 0; i < thresholds.Count; i++)
            {
                var kind = results[i];
                var escape = kind == CandidateOutcomeKind.Escape ? 1 : 0;
                var caught = kind == CandidateOutcomeKind.Caught ? 1 : 0;
                var rate = escape + caught == 0 ? 0 : (double)escape / (escape + caught);
                points.Add(new ThresholdSweepPoint(thresholds[i], escape, caught, rate));
                if (first is null && escape == 1)
                    first = thresholds[i];
            }

            perTransform[tag.ToString()] = points;
            firstEscape[tag.ToString()] = first;
        }

        return new ThresholdSensitivityReport(thresholds, perTransform, firstEscape);
    }
}

public static class EscapeRateReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task WriteJsonAsync(EscapeRateReport report, string path, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, report, JsonOptions, ct).ConfigureAwait(false);
    }

    public static async Task WriteFindingsMarkdownAsync(EscapeRateReport report, string path, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var md = RenderFindings(report);
        await File.WriteAllTextAsync(path, md, ct).ConfigureAwait(false);
    }

    public static string RenderFindings(EscapeRateReport report)
    {
        var lines = new List<string>
        {
            "# S1 Gate Escape Rate",
            "",
            "## Headline",
            "",
            $"- **Catalog version**: `{report.CatalogVersion}`",
            $"- **Adversary**: `{report.AdversaryMode}` (offline taxonomy; not adaptive/LLM)",
            $"- **Seeds**: {report.Seeds} ({report.DistinctWrongImplTrials} distinct wrong-impl trials; {report.TotalWrongImplCandidates} total runs)",
            $"- **Wrong-impl escape rate** (PropertyGate): **{report.WrongImpl.EscapeRate:P1}** " +
            $"({report.WrongImpl.Escapes}/{report.WrongImpl.Escapes + report.WrongImpl.Caught} adversarial candidates escaped)",
            $"- **Wrong-impl false-reject rate**: {report.WrongImpl.FalseRejectRate:P1}",
            $"- **Weak-test dimension**: {report.WeakTest.Status} " +
            $"(MutationGate escape rate: {FormatWeakTestRate(report.WeakTest)})",
            "",
            "## Tool availability",
            "",
            $"- dotnet: {(report.Tools.DotnetAvailable ? "available" : "missing")}",
            $"- dotnet-stryker: {(report.Tools.StrykerAvailable ? "available" : "skipped — not installed")}" +
            (report.Tools.StrykerDetail is null ? "" : $" ({report.Tools.StrykerDetail})"),
            "",
            "## Wrong-impl per-transform breakdown",
            "",
            "| Transform | Total | Escapes | Caught | Escape rate | Attribution |",
            "| --- | ---: | ---: | ---: | ---: | --- |"
        };

        foreach (var tag in TransformCatalog.WrongImplTags)
        {
            AppendTransformRow(lines, report, tag, report.WrongImpl);
        }

        if (report.WeakTest.Status.StartsWith("completed", StringComparison.Ordinal)
            || report.WeakTest.Status.StartsWith("skipped", StringComparison.Ordinal) == false)
        {
            if (report.WeakTest.TotalCandidates > 0 || report.WeakTest.Status.StartsWith("completed", StringComparison.Ordinal))
            {
                lines.Add("");
                lines.Add("## Weak-test per-transform breakdown");
                lines.Add("");
                lines.Add("| Transform | Total | Escapes | Caught | Escape rate | Attribution |");
                lines.Add("| --- | ---: | ---: | ---: | ---: | --- |");

                foreach (var tag in TransformCatalog.WeakTestTags)
                    AppendTransformRow(lines, report, tag, report.WeakTest);
            }
        }

        if (report.ThresholdSensitivity is not null && report.ThresholdSensitivity.PerTransform.Count > 0)
        {
            lines.Add("");
            lines.Add("## Threshold-sensitivity curve (weak-test)");
            lines.Add("");
            lines.Add(
                $"Thresholds swept: {string.Join(", ", report.ThresholdSensitivity.Thresholds.Select(t => $"{t:F0}%"))}. " +
                "Shows the mutation-score threshold at which each weakened test set begins to escape.");
            lines.Add("");
            lines.Add("| Transform | First escape @ | " +
                      string.Join(" | ", report.ThresholdSensitivity.Thresholds.Select(t => $"{t:F0}%")) + " |");
            lines.Add("| --- | ---: | " +
                      string.Join(" | ", report.ThresholdSensitivity.Thresholds.Select(_ => "---:")) + " |");

            foreach (var tag in TransformCatalog.WeakTestTags)
            {
                if (!report.ThresholdSensitivity.PerTransform.TryGetValue(tag.ToString(), out var points))
                    continue;

                var first = report.ThresholdSensitivity.FirstEscapeThreshold.TryGetValue(tag.ToString(), out var f)
                    ? f is null ? "never" : $"{f:F0}%"
                    : "n/a";
                var cells = points.Select(p => p.Escapes > 0 ? "escape" : "caught");
                lines.Add($"| `{tag}` | {first} | {string.Join(" | ", cells)} |");
            }
        }

        var escapes = report.Attributions
            .Where(a => a.Kind == CandidateOutcomeKind.Escape && a.Family != TransformFamily.HonestBaseline)
            .ToList();
        if (escapes.Count > 0)
        {
            lines.Add("");
            lines.Add("## Missing property relations");
            lines.Add("");
            lines.Add("Each escape names a property-oracle gap to close before self-generated bricks approach the stable surface:");
            lines.Add("");
            foreach (var escape in escapes)
            {
                lines.Add($"### `{escape.Tag}` (seed {escape.Seed}, {escape.Family})");
                lines.Add("");
                lines.Add($"- **Hypothesis**: {escape.Hypothesis}");
                lines.Add($"- **Missing relation**: {escape.MissingRelation}");
                if (!string.IsNullOrWhiteSpace(escape.DiffSummary))
                {
                    lines.Add("");
                    lines.Add("```diff");
                    lines.Add(escape.DiffSummary);
                    lines.Add("```");
                }
                lines.Add("");
            }
        }

        lines.Add("## Metric scope");
        lines.Add("");
        lines.Add(
            $"Catalog `{report.CatalogVersion}` measures escape rate for a **fixed offline transform catalog** on the S0 CSV inferencer fixtures. " +
            "Escapes are signal: each names a missing property relation. " +
            "This is not a target of 0% — attributed escapes form the property-authoring backlog. " +
            "Adaptive or LLM adversaries may find additional escapes beyond this taxonomy.");

        if (report.SurvivingExamples.Count > 0)
        {
            lines.Add("");
            lines.Add("## Surviving examples");
            lines.Add("");
            foreach (var example in report.SurvivingExamples)
            {
                lines.Add(
                    $"- `{example.Dimension}` / `{example.TransformTag}` seed {example.Seed}: " +
                    $"`{example.WorkspacePath}` — {example.MissingRelation}");
            }
        }

        return string.Join('\n', lines) + '\n';
    }

    private static string FormatWeakTestRate(DimensionReport weakTest) =>
        weakTest.Status.StartsWith("completed", StringComparison.Ordinal)
            ? $"{weakTest.EscapeRate:P1}"
            : "n/a";

    private static void AppendTransformRow(
        List<string> lines,
        EscapeRateReport report,
        TransformTag tag,
        DimensionReport dimension)
    {
        if (!dimension.PerTransform.TryGetValue(tag.ToString(), out var row))
            return;

        var adversarial = row.Escapes + row.Caught;
        var rate = adversarial == 0 ? 0 : (double)row.Escapes / adversarial;
        var sample = report.Attributions
            .Where(a => a.Tag == tag && a.Family != TransformFamily.HonestBaseline && a.Kind != CandidateOutcomeKind.Blocked)
            .Take(1)
            .FirstOrDefault();
        var attribution = sample switch
        {
            null => "n/a",
            { Kind: CandidateOutcomeKind.Escape } => $"missing: {sample.MissingRelation}",
            { Kind: CandidateOutcomeKind.Caught } => $"caught: {sample.CaughtBy}",
            _ => sample.Kind.ToString()
        };
        lines.Add($"| `{tag}` | {row.Total} | {row.Escapes} | {row.Caught} | {rate:P1} | {attribution} |");
    }
}
