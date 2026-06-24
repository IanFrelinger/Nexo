using Nexo.Core.Application.Certification.Models;

namespace Nexo.Infrastructure.Certification.Composition;

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
