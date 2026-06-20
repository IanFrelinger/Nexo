namespace Nexo.Spike.S3.Models;

/// <summary>
/// Sanitized prior gate outcome passed to generators on retry — no test/oracle output.
/// </summary>
public sealed record GateVerdictSummary(
    int AttemptIndex,
    string? FailedGate,
    string? RejectionSummary);
