using System.Text.Json;
using Nexo.CLI.Runtime;

namespace Nexo.CLI.Commands.Runtime;

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
