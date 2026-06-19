using System.Text.Json;
using System.Text.Json.Serialization;
using Nexo.Spike.S2.Adversary;
using Nexo.Spike.S2.Oracle;

namespace Nexo.Spike.S2.Reporting;

public sealed record EffortBudget(int Intents, int AttemptsPerIntent);

public sealed record NewDefectBacklogEntry(
    IReadOnlyList<string> Inputs,
    string ExpectedType,
    string ActualType,
    string CandidateId,
    int AttemptIndex,
    string Hypothesis);

public sealed record AdaptiveAttemptRecord(
    int IntentIndex,
    int AttemptIndex,
    string CandidateId,
    string Hypothesis,
    AdaptiveOutcomeKind Outcome,
    string? RejectedByGate,
    string RejectionDetail,
    IReadOnlyList<string> GatesPassed,
    IReadOnlyList<OracleDisagreement> OracleDisagreements,
    IReadOnlyList<OracleDisagreement> HeldOutDisagreements);

public sealed record AdaptiveEscapeReport(
    string ReportVersion,
    string ReferenceOracleVersion,
    string AdversaryBackend,
    string? ModelId,
    EffortBudget EffortBudget,
    string ScopeNote,
    double TrueEscapeRate,
    double BenignPassRate,
    double RejectionRate,
    int TrueEscapeCount,
    int BenignPassCount,
    int RejectedCount,
    IReadOnlyList<int?> AttemptsToFirstEscapeByIntent,
    IReadOnlyList<NewDefectBacklogEntry> NewDefectBacklog,
    IReadOnlyList<AdaptiveAttemptRecord> Attempts)
{
    public const string Version = "s2.0-v1";

    public bool NonVacuityProven =>
        TrueEscapeCount > 0 && BenignPassCount > 0;
}

public static class AdaptiveEscapeReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task WriteJsonAsync(AdaptiveEscapeReport report, string path, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, report, JsonOptions, ct).ConfigureAwait(false);
    }

    public static string RenderFindings(AdaptiveEscapeReport report)
    {
        var lines = new List<string>
        {
            "# S2 Adaptive Adversary Escape Rate",
            "",
            "## Headline",
            "",
            $"- **Report version**: `{report.ReportVersion}`",
            $"- **Reference oracle version**: `{report.ReferenceOracleVersion}`",
            $"- **Adversary backend**: `{report.AdversaryBackend}`",
            $"- **Effort budget**: {report.EffortBudget.Intents} intent(s) × {report.EffortBudget.AttemptsPerIntent} attempt(s)",
            $"- **True-escape rate**: **{report.TrueEscapeRate:P1}** ({report.TrueEscapeCount}/{report.Attempts.Count})",
            $"- **Benign-pass rate**: **{report.BenignPassRate:P1}** ({report.BenignPassCount}/{report.Attempts.Count})",
            $"- **Rejection rate**: **{report.RejectionRate:P1}** ({report.RejectedCount}/{report.Attempts.Count})",
            $"- **Non-vacuity proven**: {report.NonVacuityProven}",
            "",
            "## Scope caveat",
            "",
            report.ScopeNote,
            "",
            "## Attempts-to-first-true-escape",
            ""
        };

        for (var i = 0; i < report.AttemptsToFirstEscapeByIntent.Count; i++)
        {
            var attempt = report.AttemptsToFirstEscapeByIntent[i];
            lines.Add($"- Intent {i}: {(attempt is null ? "none detected" : attempt.ToString())}");
        }

        lines.Add("");
        lines.Add("## New-defect backlog (held-out oracle disagreements)");
        lines.Add("");

        if (report.NewDefectBacklog.Count == 0)
        {
            lines.Add("_No held-out disagreements detected._");
        }
        else
        {
            lines.Add("| Inputs | Expected | Actual | Attempt | Candidate |");
            lines.Add("| --- | --- | --- | ---: | --- |");
            foreach (var entry in report.NewDefectBacklog)
            {
                var inputs = string.Join(", ", entry.Inputs.Select(i => $"\"{i}\""));
                lines.Add($"| `[{inputs}]` | {entry.ExpectedType} | {entry.ActualType} | {entry.AttemptIndex + 1} | `{entry.CandidateId}` |");
            }
        }

        lines.Add("");
        lines.Add("## Per-attempt outcomes");
        lines.Add("");
        lines.Add("| Intent | Attempt | Candidate | Outcome | Rejected by |");
        lines.Add("| ---: | ---: | --- | --- | --- |");

        foreach (var attempt in report.Attempts)
        {
            var rejectedBy = attempt.Outcome == AdaptiveOutcomeKind.Rejected
                ? attempt.RejectedByGate ?? "unknown"
                : "—";
            lines.Add(
                $"| {attempt.IntentIndex} | {attempt.AttemptIndex + 1} | `{attempt.CandidateId}` | {attempt.Outcome} | {rejectedBy} |");
        }

        if (string.Equals(report.AdversaryBackend, AdaptiveAdversaryFactory.LlmMode, StringComparison.OrdinalIgnoreCase))
        {
            lines.Add("");
            lines.Add($"## LLM run metadata");
            lines.Add("");
            lines.Add($"- **Model id**: `{report.ModelId ?? "unspecified"}`");
        }
        else
        {
            lines.Add("");
            lines.Add("## Local LLM command");
            lines.Add("");
            lines.Add("```bash");
            lines.Add("export NEXO_S2_ADVERSARY=llm");
            lines.Add("export OPENAI_API_KEY=...   # or ANTHROPIC_API_KEY / NEXO_LLM_API_KEY");
            lines.Add("export NEXO_S2_LLM_MODEL=gpt-4o   # optional metadata recorded in report");
            lines.Add("export PATH=\"$HOME/.dotnet/tools:$PATH\"");
            lines.Add("dotnet run --project src/Nexo.Spike.S2 -- --intents 1 --attempts 8 --out artifacts/s2");
            lines.Add("```");
        }

        return string.Join('\n', lines) + '\n';
    }
}
