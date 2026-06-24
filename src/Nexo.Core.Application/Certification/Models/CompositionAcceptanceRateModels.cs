namespace Nexo.Core.Application.Certification.Models;

public sealed record CompositionAcceptanceRateEntryResult(
    int SequenceIndex,
    bool RecordedAdmitted,
    bool ReplayAdmitted,
    string? RecordedFailureCheck,
    string? ReplayFailureCheck);

public sealed record CompositionAcceptanceRateResult(
    double AcceptanceRate,
    int Admits,
    int Total,
    IReadOnlyList<CompositionAcceptanceRateEntryResult> Entries);
