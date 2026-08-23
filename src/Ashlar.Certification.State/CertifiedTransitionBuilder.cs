using System.Text.Json;

namespace Ashlar.Certification.State;

/// <summary>
/// Computes certified transition entry hashes and assembles transitions.
/// </summary>
public sealed class CertifiedTransitionBuilder
{
    public string ComputeEntryHash(
        string priorStateHash,
        string action,
        string behaviorCertContentHash,
        string resultingStateHash,
        string prevEntryHash)
    {
        var payload = new
        {
            action,
            behaviorCertContentHash,
            prevEntryHash,
            priorStateHash,
            resultingStateHash
        };

        var canonical = JsonSerializer.Serialize(
            payload,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        return Contracts.BrickContentHasher.ComputeSha256(canonical);
    }

    public CertifiedTransition Create(
        string priorStateHash,
        string action,
        string behaviorCertContentHash,
        string resultingStateHash,
        string prevEntryHash)
    {
        var entryHash = ComputeEntryHash(
            priorStateHash,
            action,
            behaviorCertContentHash,
            resultingStateHash,
            prevEntryHash);

        return new CertifiedTransition
        {
            PriorStateHash = priorStateHash,
            Action = action,
            BehaviorCertContentHash = behaviorCertContentHash,
            ResultingStateHash = resultingStateHash,
            PrevEntryHash = prevEntryHash,
            EntryHash = entryHash
        };
    }
}
