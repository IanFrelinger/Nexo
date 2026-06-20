using Nexo.Spike.S2.Adversary;
using Nexo.Spike.S2.Oracle;

namespace Nexo.Spike.S2.Reporting;

public sealed record AdaptiveEscapePublishResult(
    string RunDirectory,
    string Status,
    bool PromotedToCanonical,
    string? CanonicalJsonPath,
    string? CanonicalFindingsPath);

public static class AdaptiveEscapeReportPublisher
{
    public const string InvalidMarkerFileName = ".INVALID";
    public const string RunReportFileName = "adaptive-escape-report.json";
    public const string RunFindingsFileName = "findings.md";
    public const string CanonicalReportFileName = "adaptive-escape-report.json";
    public const string CanonicalFindingsFileName = "findings.md";

    public static string ResolveRunId(AdaptiveEscapeReport report, DateTimeOffset? timestamp = null)
    {
        var version = report.ReportVersion;
        if (string.Equals(report.AdversaryBackend, AdaptiveAdversaryFactory.MockMode, StringComparison.OrdinalIgnoreCase))
            return $"mock-{version}";

        if (string.Equals(report.AdversaryBackend, AdaptiveAdversaryFactory.ScriptedStandInMode, StringComparison.OrdinalIgnoreCase))
            return $"scripted-standin-{version}";

        if (string.Equals(report.AdversaryBackend, AdaptiveAdversaryFactory.LlmMode, StringComparison.OrdinalIgnoreCase))
        {
            var model = SanitizeRunIdSegment(report.ModelId ?? "unspecified");
            var stamp = (timestamp ?? DateTimeOffset.UtcNow).ToString("yyyyMMddTHHmmssZ");
            return $"llm-{model}-{stamp}";
        }

        return $"{SanitizeRunIdSegment(report.AdversaryBackend)}-{version}";
    }

    public static string ResolveStubLlmRunId(string? modelHint = null, DateTimeOffset? timestamp = null)
    {
        var model = SanitizeRunIdSegment(modelHint ?? "unspecified");
        var stamp = (timestamp ?? DateTimeOffset.UtcNow).ToString("yyyyMMddTHHmmssZ");
        return $"llm-{model}-stub-{stamp}";
    }

    public static string ResolveRunDirectory(string artifactsRoot, string runId) =>
        Path.Combine(artifactsRoot, "runs", runId);

    public static string PlanRunDirectory(string artifactsRoot, string adversaryMode, DateTimeOffset timestamp)
    {
        var runId = adversaryMode switch
        {
            var mode when string.Equals(mode, AdaptiveAdversaryFactory.MockMode, StringComparison.OrdinalIgnoreCase)
                => $"mock-{AdaptiveEscapeReport.Version}",
            var mode when string.Equals(mode, AdaptiveAdversaryFactory.ScriptedStandInMode, StringComparison.OrdinalIgnoreCase)
                => $"scripted-standin-{AdaptiveEscapeReport.Version}",
            var mode when string.Equals(mode, AdaptiveAdversaryFactory.LlmMode, StringComparison.OrdinalIgnoreCase)
                => ResolveRunId(
                    new AdaptiveEscapeReport(
                        AdaptiveEscapeReport.Version,
                        ReferenceOracle.VersionId,
                        AdaptiveAdversaryFactory.LlmMode,
                        Environment.GetEnvironmentVariable("NEXO_S2_LLM_MODEL"),
                        new EffortBudget(1, 1),
                        AdaptiveEscapeHarness.ScopeNote,
                        0, 0, 0, 0, 0, 0, [], [], []),
                    timestamp),
            _ => $"{SanitizeRunIdSegment(adversaryMode)}-{AdaptiveEscapeReport.Version}"
        };

        return ResolveRunDirectory(artifactsRoot, runId);
    }

    public static string DetermineStatus(AdaptiveEscapeReport report) =>
        report.NonVacuityProven
            ? AdaptiveEscapeRunStatus.Valid
            : AdaptiveEscapeRunStatus.InvalidVacuous;

    public static bool IsCanonicalEligible(AdaptiveEscapeReport report) =>
        report.NonVacuityProven
        && string.Equals(report.AdversaryBackend, AdaptiveAdversaryFactory.MockMode, StringComparison.OrdinalIgnoreCase);

    public static void PromoteToCanonical(AdaptiveEscapeReport report, string artifactsRoot)
    {
        if (!report.NonVacuityProven)
        {
            throw new InvalidOperationException(
                "Cannot promote a vacuous run to canonical adaptive-escape-report.json. " +
                "A true-escape rate requires nonVacuityProven=true in the same run.");
        }

        if (!IsCanonicalEligible(report))
        {
            throw new InvalidOperationException(
                $"Backend '{report.AdversaryBackend}' cannot be promoted to the canonical headline report.");
        }

        var canonicalJson = Path.Combine(artifactsRoot, CanonicalReportFileName);
        var canonicalMd = Path.Combine(artifactsRoot, CanonicalFindingsFileName);
        WriteCanonicalFiles(report, artifactsRoot, canonicalJson, canonicalMd);
    }

    public static async Task<AdaptiveEscapePublishResult> PublishRunAsync(
        AdaptiveEscapeReport report,
        string artifactsRoot,
        string? runDirectory = null,
        string? statusDetail = null,
        bool attemptCanonicalPromotion = true,
        CancellationToken ct = default)
    {
        var status = DetermineStatus(report);
        runDirectory ??= ResolveRunDirectory(artifactsRoot, ResolveRunId(report));
        Directory.CreateDirectory(runDirectory);

        var detail = statusDetail ?? BuildDefaultStatusDetail(report, status);
        await WriteRunRecordAsync(report, runDirectory, status, detail, ct).ConfigureAwait(false);

        if (status != AdaptiveEscapeRunStatus.Valid)
        {
            await File.WriteAllTextAsync(
                Path.Combine(runDirectory, InvalidMarkerFileName),
                detail + Environment.NewLine,
                ct).ConfigureAwait(false);
        }

        if (attemptCanonicalPromotion && IsCanonicalEligible(report))
        {
            PromoteToCanonical(report, artifactsRoot);
            return new AdaptiveEscapePublishResult(
                runDirectory,
                status,
                PromotedToCanonical: true,
                Path.Combine(artifactsRoot, CanonicalReportFileName),
                Path.Combine(artifactsRoot, CanonicalFindingsFileName));
        }

        return new AdaptiveEscapePublishResult(
            runDirectory,
            status,
            PromotedToCanonical: false,
            null,
            null);
    }

    public static async Task<AdaptiveEscapePublishResult> PublishStubLlmFailureAsync(
        string artifactsRoot,
        string statusDetail,
        string? modelHint = null,
        CancellationToken ct = default)
    {
        var runId = ResolveStubLlmRunId(modelHint);
        var runDirectory = ResolveRunDirectory(artifactsRoot, runId);
        Directory.CreateDirectory(runDirectory);

        var emptyReport = BuildEmptyLlmReport(modelHint);
        await WriteRunRecordAsync(
            emptyReport,
            runDirectory,
            AdaptiveEscapeRunStatus.InvalidStubLlm,
            statusDetail,
            ct).ConfigureAwait(false);

        await File.WriteAllTextAsync(
            Path.Combine(runDirectory, InvalidMarkerFileName),
            statusDetail + Environment.NewLine,
            ct).ConfigureAwait(false);

        return new AdaptiveEscapePublishResult(
            runDirectory,
            AdaptiveEscapeRunStatus.InvalidStubLlm,
            PromotedToCanonical: false,
            null,
            null);
    }

    private static async Task WriteRunRecordAsync(
        AdaptiveEscapeReport report,
        string runDirectory,
        string status,
        string? statusDetail,
        CancellationToken ct)
    {
        var record = new AdaptiveEscapeRunRecord(status, statusDetail, report);
        var jsonPath = Path.Combine(runDirectory, RunReportFileName);
        var mdPath = Path.Combine(runDirectory, RunFindingsFileName);

        await AdaptiveEscapeReportWriter.WriteRunRecordAsync(record, jsonPath, ct).ConfigureAwait(false);
        await File.WriteAllTextAsync(
            mdPath,
            AdaptiveEscapeReportWriter.RenderFindings(report, BuildRunFindingsOptions(report, status)),
            ct).ConfigureAwait(false);
    }

    private static void WriteCanonicalFiles(
        AdaptiveEscapeReport report,
        string artifactsRoot,
        string canonicalJson,
        string canonicalMd)
    {
        Directory.CreateDirectory(artifactsRoot);
        AdaptiveEscapeReportWriter.WriteCanonicalJson(report, canonicalJson);
        File.WriteAllText(
            canonicalMd,
            AdaptiveEscapeReportWriter.RenderFindings(
                report,
                AdaptiveEscapeFindingsOptions.ForCanonical()));
    }

    private static AdaptiveEscapeFindingsOptions BuildRunFindingsOptions(
        AdaptiveEscapeReport report,
        string status) =>
        string.Equals(report.AdversaryBackend, AdaptiveAdversaryFactory.ScriptedStandInMode, StringComparison.OrdinalIgnoreCase)
            ? AdaptiveEscapeFindingsOptions.ForScriptedStandInRun(status)
            : AdaptiveEscapeFindingsOptions.ForRun(status);

    private static string BuildDefaultStatusDetail(AdaptiveEscapeReport report, string status) =>
        status switch
        {
            AdaptiveEscapeRunStatus.InvalidVacuous =>
                "Run did not prove non-vacuity (requires both TrueEscape and BenignPass in the same run). " +
                "True-escape rate from this run is not headline-valid.",
            _ => "Run completed with non-vacuity proven."
        };

    private static AdaptiveEscapeReport BuildEmptyLlmReport(string? modelHint) =>
        new(
            AdaptiveEscapeReport.Version,
            ReferenceOracle.VersionId,
            AdaptiveAdversaryFactory.LlmMode,
            modelHint,
            new EffortBudget(0, 0),
            AdaptiveEscapeHarness.ScopeNote,
            0,
            0,
            0,
            0,
            0,
            0,
            [],
            [],
            []);

    private static string SanitizeRunIdSegment(string value)
    {
        var chars = value
            .Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.' ? ch : '-')
            .ToArray();
        var sanitized = new string(chars).Trim('-');
        return string.IsNullOrWhiteSpace(sanitized) ? "unspecified" : sanitized;
    }
}
