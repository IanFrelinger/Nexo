namespace Ashlar.Core.Application.Certification.Models;

/// <summary>Per-sequence acceptance comparison between recorded and replayed certification.</summary>
/// <param name="SequenceIndex">Index within the certification sequence under test.</param>
/// <param name="RecordedAdmitted">Whether the original record admitted this sequence.</param>
/// <param name="ReplayAdmitted">Whether replay certification admitted this sequence.</param>
/// <param name="RecordedFailureCheck">Failure check id from the original record, when any.</param>
/// <param name="ReplayFailureCheck">Failure check id from replay, when any.</param>
public sealed record CompositionAcceptanceRateEntryResult(
    int SequenceIndex,
    bool RecordedAdmitted,
    bool ReplayAdmitted,
    string? RecordedFailureCheck,
    string? ReplayFailureCheck);
