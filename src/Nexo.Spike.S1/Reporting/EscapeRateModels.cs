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
    string? Detail);

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

public sealed record SurvivingExample(
    string Dimension,
    string TransformTag,
    int Seed,
    string WorkspacePath,
    string? DiffSummary);

public sealed record EscapeRateReport(
    string ReportVersion,
    string AdversaryMode,
    int Seeds,
    int MutationSample,
    int BudgetMinutes,
    ToolAvailability Tools,
    DimensionReport WrongImpl,
    DimensionReport WeakTest,
    IReadOnlyList<SurvivingExample> SurvivingExamples)
{
    public const string Version = "s1-v1";
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
}

public static class EscapeRateReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
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
            $"- **Adversary**: `{report.AdversaryMode}` (offline taxonomy = lower bound; not adaptive/LLM)",
            $"- **Seeds**: {report.Seeds}",
            $"- **Wrong-impl escape rate** (PropertyGate): **{report.WrongImpl.EscapeRate:P1}** " +
            $"({report.WrongImpl.Escapes}/{report.WrongImpl.Escapes + report.WrongImpl.Caught} adversarial candidates escaped)",
            $"- **Wrong-impl false-reject rate**: {report.WrongImpl.FalseRejectRate:P1}",
            $"- **Weak-test dimension**: {report.WeakTest.Status} " +
            $"(MutationGate escape rate: {(report.WeakTest.Status == "completed" ? $"{report.WeakTest.EscapeRate:P1}" : "n/a")})",
            "",
            "## Tool availability",
            "",
            $"- dotnet: {(report.Tools.DotnetAvailable ? "available" : "missing")}",
            $"- dotnet-stryker: {(report.Tools.StrykerAvailable ? "available" : "skipped — not installed")}" +
            (report.Tools.StrykerDetail is null ? "" : $" ({report.Tools.StrykerDetail})"),
            "",
            "## Wrong-impl per-transform breakdown",
            "",
            "| Transform | Total | Escapes | Caught | Escape rate |",
            "| --- | ---: | ---: | ---: | ---: |"
        };

        foreach (var tag in TransformCatalog.WrongImplTags)
        {
            if (!report.WrongImpl.PerTransform.TryGetValue(tag.ToString(), out var row))
                continue;

            var adversarial = row.Escapes + row.Caught;
            var rate = adversarial == 0 ? 0 : (double)row.Escapes / adversarial;
            lines.Add($"| `{tag}` | {row.Total} | {row.Escapes} | {row.Caught} | {rate:P1} |");
        }

        if (report.WeakTest.Status == "completed")
        {
            lines.Add("");
            lines.Add("## Weak-test per-transform breakdown");
            lines.Add("");
            lines.Add("| Transform | Total | Escapes | Caught | Escape rate |");
            lines.Add("| --- | ---: | ---: | ---: | ---: |");

            foreach (var tag in TransformCatalog.WeakTestTags)
            {
                if (!report.WeakTest.PerTransform.TryGetValue(tag.ToString(), out var row))
                    continue;

                var adversarial = row.Escapes + row.Caught;
                var rate = adversarial == 0 ? 0 : (double)row.Escapes / adversarial;
                lines.Add($"| `{tag}` | {row.Total} | {row.Escapes} | {row.Caught} | {rate:P1} |");
            }
        }

        lines.Add("");
        lines.Add("## Metric scope");
        lines.Add("");
        lines.Add(
            "This report measures escape rate for a **fixed offline transform catalog** applied to the S0 CSV inferencer fixtures. " +
            "It is a deterministic lower bound: adaptive or LLM-generated adversaries may find additional escapes. " +
            "False-reject counts come from honest no-op baselines run through the same gates.");

        if (report.SurvivingExamples.Count > 0)
        {
            lines.Add("");
            lines.Add("## Surviving examples");
            lines.Add("");
            foreach (var example in report.SurvivingExamples)
            {
                lines.Add($"- `{example.Dimension}` / `{example.TransformTag}` seed {example.Seed}: `{example.WorkspacePath}`");
            }
        }

        return string.Join('\n', lines) + '\n';
    }
}
