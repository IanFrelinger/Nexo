using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ashlar.Infrastructure.Certification.Composition;

/// <summary>
/// Ordered, sequence-indexed batch of live model proposals and their gate verdicts at recording time.
/// </summary>
public sealed record RecordedCompositionProposalBatch(
    string Provider,
    double Temperature,
    int DeclaredSampleCount,
    DateTimeOffset BatchRecordedAt,
    string Discards,
    IReadOnlyList<RecordedCompositionBatchEntry> Entries)
{
    /// <summary>To json.</summary>
    public string ToJson() => JsonSerializer.Serialize(
        new RecordedCompositionProposalBatchDto
        {
            Provider = Provider,
            Temperature = Temperature,
            DeclaredSampleCount = DeclaredSampleCount,
            BatchRecordedAt = BatchRecordedAt,
            Discards = Discards,
            Entries = Entries.Select(ToDto).ToList()
        },
        JsonOptions);

    /// <summary>From json.</summary>
    public static RecordedCompositionProposalBatch FromJson(string json)
    {
        var dto = JsonSerializer.Deserialize<RecordedCompositionProposalBatchDto>(json, JsonOptions)
            ?? throw new InvalidOperationException("Recorded composition batch JSON is empty.");

        return new RecordedCompositionProposalBatch(
            dto.Provider,
            dto.Temperature,
            dto.DeclaredSampleCount,
            dto.BatchRecordedAt,
            dto.Discards,
            dto.Entries?.Select(FromDto).ToList()
                ?? throw new InvalidOperationException("Recorded composition batch missing entries."));
    }

    private static RecordedCompositionBatchEntryDto ToDto(RecordedCompositionBatchEntry entry) =>
        new()
        {
            SequenceIndex = entry.SequenceIndex,
            Provenance = entry.Provenance,
            RecordedAt = entry.RecordedAt,
            GateAdmittedAtRecording = entry.GateAdmittedAtRecording,
            GateVerdictAtRecording = entry.GateVerdictAtRecording,
            GateFailureCheckAtRecording = entry.GateFailureCheckAtRecording,
            Spec = CompositionSpecJsonParser.ToDto(entry.Spec)
        };

    private static RecordedCompositionBatchEntry FromDto(RecordedCompositionBatchEntryDto dto) =>
        new(
            dto.SequenceIndex,
            dto.Provenance,
            dto.RecordedAt,
            dto.GateAdmittedAtRecording,
            dto.GateVerdictAtRecording,
            dto.GateFailureCheckAtRecording,
            CompositionSpecJsonParser.FromDto(dto.Spec));

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private sealed class RecordedCompositionProposalBatchDto
    {
        /// <summary>Provider.</summary>
        public string Provider { get; set; } = "";
        /// <summary>Temperature.</summary>
        public double Temperature { get; set; }
        /// <summary>Declared sample count.</summary>
        public int DeclaredSampleCount { get; set; }
        /// <summary>Batch recorded at.</summary>
        public DateTimeOffset BatchRecordedAt { get; set; }
        /// <summary>Discards.</summary>
        public string Discards { get; set; } = "none";
        /// <summary>Entries.</summary>
        public List<RecordedCompositionBatchEntryDto>? Entries { get; set; }
    }

    private sealed class RecordedCompositionBatchEntryDto
    {
        /// <summary>Sequence index.</summary>
        public int SequenceIndex { get; set; }
        /// <summary>Provenance.</summary>
        public string Provenance { get; set; } = "";
        /// <summary>Recorded at.</summary>
        public DateTimeOffset RecordedAt { get; set; }
        /// <summary>Gate admitted at recording.</summary>
        public bool GateAdmittedAtRecording { get; set; }
        /// <summary>Gate verdict at recording.</summary>
        public string GateVerdictAtRecording { get; set; } = "";
        /// <summary>Gate failure check at recording.</summary>
        public string? GateFailureCheckAtRecording { get; set; }
        /// <summary>Spec.</summary>
        public CompositionSpecJsonParser.CompositionSpecDto Spec { get; set; } = new();
    }
}
