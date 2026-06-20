namespace Nexo.Spike.S3.Models;

/// <summary>
/// Sealed generation handoff payload — intent only plus sanitized prior verdicts on retry.
/// </summary>
public sealed record GenerationRequest(
    IntentDescriptor Intent,
    SealedIntentSpec IntentSpec,
    IReadOnlyList<GateVerdictSummary> PriorGateVerdicts,
    int AttemptIndex);
