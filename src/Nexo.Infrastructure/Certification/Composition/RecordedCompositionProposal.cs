using System.Text.Json;
using System.Text.Json.Serialization;
using Nexo.Core.Application.Certification.Models;

namespace Nexo.Infrastructure.Certification.Composition;

/// <summary>
/// Serializable recorded composition proposal for offline replay in cert-gate.
/// Produced by a one-time local live call via <see cref="CompositionProposalRecorder"/>.
/// </summary>
public sealed record RecordedCompositionProposal(
    string Provenance,
    string Provider,
    DateTimeOffset RecordedAt,
    string CompositionId,
    bool FirstTryCertified,
    string GateVerdictAtRecording,
    string? GateFailureCheckAtRecording,
    CompositionSpec Spec)
{
    public string ToJson() => JsonSerializer.Serialize(
        new RecordedCompositionProposalDto
        {
            Provenance = Provenance,
            Provider = Provider,
            RecordedAt = RecordedAt,
            CompositionId = CompositionId,
            FirstTryCertified = FirstTryCertified,
            GateVerdictAtRecording = GateVerdictAtRecording,
            GateFailureCheckAtRecording = GateFailureCheckAtRecording,
            Spec = CompositionSpecJsonParser.ToDto(Spec)
        },
        JsonOptions);

    public static RecordedCompositionProposal FromJson(string json)
    {
        var dto = JsonSerializer.Deserialize<RecordedCompositionProposalDto>(json, JsonOptions)
            ?? throw new InvalidOperationException("Recorded composition proposal JSON is empty.");

        return new RecordedCompositionProposal(
            dto.Provenance,
            dto.Provider,
            dto.RecordedAt,
            dto.CompositionId,
            dto.FirstTryCertified,
            dto.GateVerdictAtRecording,
            dto.GateFailureCheckAtRecording,
            CompositionSpecJsonParser.FromDto(dto.Spec));
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private sealed class RecordedCompositionProposalDto
    {
        public string Provenance { get; set; } = "";
        public string Provider { get; set; } = "";
        public DateTimeOffset RecordedAt { get; set; }
        public string CompositionId { get; set; } = "";
        public bool FirstTryCertified { get; set; }
        public string GateVerdictAtRecording { get; set; } = "";
        public string? GateFailureCheckAtRecording { get; set; }
        public CompositionSpecJsonParser.CompositionSpecDto Spec { get; set; } = new();
    }
}
