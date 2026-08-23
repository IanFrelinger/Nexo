using Ashlar.Core.Application.Certification.Models;

namespace Ashlar.Infrastructure.Certification.Composition;

/// <summary>
/// One sequence-indexed sample from a live proposal batch recording.
/// </summary>
public sealed record RecordedCompositionBatchEntry(
    int SequenceIndex,
    string Provenance,
    DateTimeOffset RecordedAt,
    bool GateAdmittedAtRecording,
    string GateVerdictAtRecording,
    string? GateFailureCheckAtRecording,
    CompositionSpec Spec);
