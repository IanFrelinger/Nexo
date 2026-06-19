using System.Text.Json;
using System.Text.Json.Serialization;
using Nexo.Spike.S1.Adversary;
using Nexo.Spike.S1.IntentDensity;

namespace Nexo.Spike.S1.Reporting;

public enum CertificationVerdict
{
    Certifiable,
    CertifiableWithScope,
    NotCertifiable
}

public sealed record ProbeSeedWitnessResult(
    int Seed,
    bool HonestAccepted,
    bool DivergentAccepted,
    bool BehaviorallyIdentical,
    bool PinnedForSeed);

public sealed record ProbeClassResult(
    string ProbeClassId,
    string Description,
    ProbePinStatus Status,
    string DecidingRelation,
    TransformTag DivergentTransform,
    bool HonestAccepted,
    bool DivergentAccepted,
    int WitnessSeedCount,
    IReadOnlyList<int> EscapingSeeds,
    IReadOnlyList<int> VacuousSeeds,
    IReadOnlyList<ProbeSeedWitnessResult> SeedWitnesses);

public sealed record CertificationResult(
    CertificationVerdict Verdict,
    double IntentDensity,
    double Threshold,
    string Message,
    IReadOnlyList<string> PinnedClasses,
    IReadOnlyList<string> UnpinnedClasses,
    IReadOnlyList<string> OutOfScopeClasses);

public sealed record CertificationEquivalenceReport(
    bool DensityEqualsOne,
    bool EscapesEqualsZero,
    bool EquivalenceHolds,
    double IntentDensity,
    int WrongImplEscapes,
    string Scope);

public sealed record NegativeControlReport(
    bool Enabled,
    string? RemovedRelation,
    double IntentDensity,
    int WrongImplEscapes,
    bool EquivalenceBroken);

public sealed record IntentDensityReport(
    string ReportVersion,
    string ProbeCorpusVersion,
    string CatalogVersion,
    int ConfiguredSeedCount,
    double IntentDensity,
    int PinnedCount,
    int UnpinnedCount,
    int TotalProbeClasses,
    double CertificationThreshold,
    CertificationResult Certification,
    CertificationEquivalenceReport? Equivalence,
    NegativeControlReport? NegativeControl,
    IReadOnlyList<ProbeClassResult> ProbeClasses)
{
    public const string Version = "s1.4-v1";
}

public static class IntentDensityReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task WriteJsonAsync(IntentDensityReport report, string path, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, report, JsonOptions, ct).ConfigureAwait(false);
    }

    public static string RenderFindingsSection(IntentDensityReport report)
    {
        var lines = new List<string>
        {
            "## Intent density (multi-witness)",
            "",
            $"- **Scope**: {CertificationEquivalence.ScopeNote}",
            $"- **Probe corpus version**: `{report.ProbeCorpusVersion}`",
            $"- **Witness seeds per class**: {report.ConfiguredSeedCount}",
            $"- **Intent density**: **{report.IntentDensity:P1}** ({report.PinnedCount}/{report.TotalProbeClasses} probe classes pinned across all seeds)",
            $"- **Certification threshold**: {report.CertificationThreshold:P0}",
            $"- **Honest-impl certification**: **{report.Certification.Verdict}** — {report.Certification.Message}",
            "",
            "| Probe class | Status | Witnesses | Escaping seeds | Deciding relation |",
            "| --- | --- | ---: | --- | --- |"
        };

        foreach (var probe in report.ProbeClasses)
        {
            var escaping = probe.EscapingSeeds.Count == 0
                ? "—"
                : string.Join(", ", probe.EscapingSeeds);
            lines.Add(
                $"| `{probe.ProbeClassId}` | {probe.Status} | {probe.WitnessSeedCount} | {escaping} | {probe.DecidingRelation} |");
        }

        if (report.Equivalence is not null)
        {
            lines.Add("");
            lines.Add("### Certification equivalence (capstone)");
            lines.Add("");
            lines.Add("| density==1.0 | escapes==0 | equivalence holds |");
            lines.Add("| --- | --- | --- |");
            lines.Add(
                $"| {report.Equivalence.DensityEqualsOne} | {report.Equivalence.EscapesEqualsZero} | **{report.Equivalence.EquivalenceHolds}** |");
        }

        if (report.NegativeControl is { Enabled: true } control)
        {
            lines.Add("");
            lines.Add("### Negative control (one relation removed)");
            lines.Add("");
            lines.Add($"- **Removed**: `{control.RemovedRelation}`");
            lines.Add($"- **Intent density**: {control.IntentDensity:P1}");
            lines.Add($"- **Wrong-impl escapes**: {control.WrongImplEscapes}");
            lines.Add($"- **Equivalence broken (expected)**: {control.EquivalenceBroken}");
        }

        if (report.Certification.UnpinnedClasses.Count > 0)
        {
            lines.Add("");
            lines.Add("### Densification backlog (unpinned probe classes)");
            lines.Add("");
            foreach (var unpinned in report.Certification.UnpinnedClasses)
                lines.Add($"- `{unpinned}`");
        }

        return string.Join('\n', lines) + '\n';
    }
}
