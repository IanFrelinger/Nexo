using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nexo.Certification.State;

/// <summary>
/// Portable JSON wire format for attested state logs and schemas.
/// </summary>
public static class AttestedStateLogWireFormat
{
    public static string SerializeSchema(StateSchema schema) =>
        schema.CanonicalForm;

    public static StateSchema DeserializeSchema(string canonicalForm) =>
        new(canonicalForm);

    public static string SerializeLog(AttestedStateLog log)
    {
        var dto = new AttestedStateLogDto
        {
            Transitions = log.Transitions
                .Select(transition => new CertifiedTransitionDto
                {
                    PriorStateHash = transition.PriorStateHash,
                    Action = transition.Action,
                    BehaviorCertContentHash = transition.BehaviorCertContentHash,
                    ResultingStateHash = transition.ResultingStateHash,
                    PrevEntryHash = transition.PrevEntryHash,
                    EntryHash = transition.EntryHash
                })
                .ToArray()
        };

        return JsonSerializer.Serialize(dto, JsonOptions);
    }

    public static AttestedStateLog DeserializeLog(string json)
    {
        var dto = JsonSerializer.Deserialize<AttestedStateLogDto>(json, JsonOptions)
            ?? throw new JsonException("Attested state log JSON is invalid.");

        var transitions = dto.Transitions
            .Select(item => new CertifiedTransition
            {
                PriorStateHash = item.PriorStateHash,
                Action = item.Action,
                BehaviorCertContentHash = item.BehaviorCertContentHash,
                ResultingStateHash = item.ResultingStateHash,
                PrevEntryHash = item.PrevEntryHash,
                EntryHash = item.EntryHash
            })
            .ToArray();

        return new AttestedStateLog(transitions);
    }

    public static string SerializeLiveState(LiveStateSnapshot snapshot) =>
        JsonSerializer.Serialize(
            new LiveStateDto { CurrentStateHash = snapshot.CurrentStateHash },
            JsonOptions);

    public static LiveStateSnapshot DeserializeLiveState(string json)
    {
        var dto = JsonSerializer.Deserialize<LiveStateDto>(json, JsonOptions)
            ?? throw new JsonException("Live state JSON is invalid.");

        if (string.IsNullOrWhiteSpace(dto.CurrentStateHash))
            throw new JsonException("Live state currentStateHash is required.");

        return new LiveStateSnapshot(dto.CurrentStateHash);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    private sealed class AttestedStateLogDto
    {
        public CertifiedTransitionDto[] Transitions { get; set; } = Array.Empty<CertifiedTransitionDto>();
    }

    private sealed class CertifiedTransitionDto
    {
        public string PriorStateHash { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string BehaviorCertContentHash { get; set; } = string.Empty;
        public string ResultingStateHash { get; set; } = string.Empty;
        public string PrevEntryHash { get; set; } = string.Empty;
        public string EntryHash { get; set; } = string.Empty;
    }

    private sealed class LiveStateDto
    {
        public string CurrentStateHash { get; set; } = string.Empty;
    }
}
