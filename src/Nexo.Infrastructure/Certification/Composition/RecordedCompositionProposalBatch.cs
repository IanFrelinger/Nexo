using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nexo.Infrastructure.Certification.Composition;

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
        public string Provider { get; set; } = "";
        public double Temperature { get; set; }
        public int DeclaredSampleCount { get; set; }
        public DateTimeOffset BatchRecordedAt { get; set; }
        public string Discards { get; set; } = "none";
        public List<RecordedCompositionBatchEntryDto>? Entries { get; set; }
    }

    private sealed class RecordedCompositionBatchEntryDto
    {
        public int SequenceIndex { get; set; }
        public string Provenance { get; set; } = "";
        public DateTimeOffset RecordedAt { get; set; }
        public bool GateAdmittedAtRecording { get; set; }
        public string GateVerdictAtRecording { get; set; } = "";
        public string? GateFailureCheckAtRecording { get; set; }
        public CompositionSpecJsonParser.CompositionSpecDto Spec { get; set; } = new();
    }
}
