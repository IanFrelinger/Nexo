using System.Text.Json;
using Nexo.CLI.Runtime;

namespace Nexo.CLI.Commands;

internal sealed record RuntimeSubprocessResult(int ExitCode, string StdOut, string StdErr);

internal sealed record RuntimeVisualInfraCapability(bool Ready, string Summary);


internal sealed record RuntimeExecuteResult(
    bool Ok,
    string Summary,
    AdaptiveRuntimeExecutionPlan? Plan = null,
    BootstrapAssessment? BootstrapBefore = null,
    BootstrapAssessment? BootstrapAfter = null,
    bool BootstrapApplied = false,
    int? BootstrapApplyExit = null,
    JsonElement? PreflightPayload = null,
    RuntimeSubprocessResult? PreflightRun = null,
    JsonElement? SelfExtendPayload = null,
    RuntimeSubprocessResult? SelfExtendRun = null,
    string? RequestedQaPolicy = null,
    string? ResolvedQaPolicy = null,
    string? AdaptivePolicyReason = null,
    string? RepoRoot = null,
    string? FailureStage = null,
    bool? BootstrapOk = null,
    bool? PreflightRan = null,
    bool? PreflightOk = null,
    bool? SelfExtendRan = null,
    bool? SelfExtendOk = null,
    string? RunId = null,
    DateTimeOffset? StartedAtUtc = null,
    long? ElapsedMs = null,
    string? GoalFingerprint = null,
    string? GoalPreview = null,
    RuntimeRemediationAttempt[]? RemediationAttempts = null);


internal sealed record RuntimePlanResult(
    bool Ok,
    string Summary,
    AdaptiveRuntimeExecutionPlan? Plan = null,
    SelfExtendWorkflowRuntimeSpec? WorkflowSpec = null,
    string? AdaptivePolicyReason = null);


internal sealed record RuntimePlanContext(
    string Goal,
    AdaptiveRuntimeManifest Manifest,
    AdaptiveRuntimeExecutionPlan Plan,
    SelfExtendWorkflowRuntimeSpec WorkflowSpec,
    string RequestedQaPolicy,
    string? AdaptivePolicyReason);


internal sealed record RuntimeEvaluateScenarioResult(
    string Goal,
    string Policy,
    bool Ok,
    long? ElapsedMs,
    string? FailureStage,
    string Summary);


internal sealed record RuntimeEvaluatePolicySummary(
    string Policy,
    int Total,
    int Passed,
    int Failed,
    long AverageElapsedMs);


internal sealed record RuntimeEvaluateResult(
    bool Ok,
    string Summary,
    IReadOnlyList<RuntimeEvaluateScenarioResult>? Scenarios = null,
    IReadOnlyList<RuntimeEvaluatePolicySummary>? PolicySummaries = null);


internal sealed record RuntimeHistorySummary(
    int Total,
    int Passed,
    int Failed,
    long AverageElapsedMs);


internal sealed record RuntimeHistoryResult(
    bool Ok,
    string Summary,
    IReadOnlyList<AdaptiveRuntimeExecutionReport>? Items = null,
    RuntimeHistorySummary? SummaryStats = null);


internal sealed record RuntimeRecommendResult(
    bool Ok,
    string Summary,
    string? Policy = null,
    string? Reason = null);


internal sealed record RuntimeGateResult(
    bool Ok,
    string Summary,
    int? Total = null,
    int? Passed = null,
    double? PassRate = null,
    int? MinTotal = null,
    double? MinPassRate = null,
    int? Streak = null,
    int? MinConsecutivePasses = null,
    RuntimeSloEvidence? SloEvidence = null);


internal sealed record RuntimeSloEvidence(
    DateTimeOffset GeneratedAtUtc,
    string Mode,
    bool VisualLaneRequired,
    RuntimeSloThresholds Thresholds,
    IReadOnlyList<RuntimeSloCheck> Checks,
    IReadOnlyList<RuntimeLaneSloEvidence> Lanes,
    int TotalSamples,
    double NcrResolutionP95Ms,
    double NcrLoadP95Ms,
    double NcrOutcomeP95Ms,
    double NcrFailureRate,
    bool Passed);


internal sealed record RuntimeSloThresholds(
    double NcrResolutionP95MsMax,
    double NcrLoadP95MsMax,
    double NcrOutcomeP95MsMax,
    double NcrFailureRateMax);


internal sealed record RuntimeSloCheck(
    string Name,
    double Actual,
    double Threshold,
    bool Passed,
    string Detail);


internal sealed record RuntimeLaneSloEvidence(
    string Lane,
    string BenchmarkSet,
    bool? GateOk,
    string? GateSummary,
    double? PassRate,
    double? MinPassRate,
    int? Total,
    int? PassedCount);


internal sealed record RuntimeRemediationPolicy(string Policy, string Reason);


internal sealed record RuntimeRemediationAttempt(
    string Policy,
    string Reason,
    bool Ok,
    string? FailureStage,
    string? RunId,
    string Summary);

