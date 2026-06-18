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

public sealed record ProbeClassResult(
    string ProbeClassId,
    string Description,
    ProbePinStatus Status,
    string DecidingRelation,
    TransformTag DivergentTransform,
    bool HonestAccepted,
    bool DivergentAccepted);

public sealed record CertificationResult(
    CertificationVerdict Verdict,
    double IntentDensity,
    double Threshold,
    string Message,
    IReadOnlyList<string> PinnedClasses,
    IReadOnlyList<string> UnpinnedClasses,
    IReadOnlyList<string> OutOfScopeClasses);

public sealed record IntentDensityReport(
    string ReportVersion,
    string ProbeCorpusVersion,
    string CatalogVersion,
    double IntentDensity,
    int PinnedCount,
    int UnpinnedCount,
    int TotalProbeClasses,
    double CertificationThreshold,
    CertificationResult Certification,
    IReadOnlyList<ProbeClassResult> ProbeClasses)
{
    public const string Version = "s1.2-v1";
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
            "## Intent density",
            "",
            $"- **Probe corpus version**: `{report.ProbeCorpusVersion}`",
            $"- **Intent density**: **{report.IntentDensity:P1}** ({report.PinnedCount}/{report.TotalProbeClasses} probe classes pinned)",
            $"- **Certification threshold**: {report.CertificationThreshold:P0}",
            $"- **Honest-impl certification**: **{report.Certification.Verdict}** — {report.Certification.Message}",
            "",
            "| Probe class | Status | Deciding relation |",
            "| --- | --- | --- |"
        };

        foreach (var probe in report.ProbeClasses)
        {
            lines.Add($"| `{probe.ProbeClassId}` | {probe.Status} | {probe.DecidingRelation} |");
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
