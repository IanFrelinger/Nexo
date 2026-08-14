using Nexo.Certification.Contracts;

namespace Nexo.Infrastructure.Certification;

/// <summary>
/// §8 depth laundering, record-store leg: a depth-n artifact re-minted through a fresh
/// session claiming human-authored context produces a SECOND certificate for the same
/// content hash whose inputs carry no (or a lower) generation-depth claim. The lineage
/// itself cannot see that lie — the input-chain record can. This detector finds content
/// hashes certified at contradictory depths so the operator (or the digest job) can
/// revoke the laundered certificate.
/// </summary>
public static class DepthLaunderingDetector
{
    /// <summary>One content hash with contradictory recorded depths across its certificates.</summary>
    public sealed record Contradiction(string ContentHash, IReadOnlyList<int> RecordedDepths);

    /// <summary>
    /// Content hashes whose PASS certificates record more than one distinct generation
    /// depth (absent lineage input = depth 0: the human-authored claim IS a depth claim).
    /// </summary>
    public static IReadOnlyList<Contradiction> FindContradictions(
        IReadOnlyCollection<CertificationRecordData> records)
    {
        if (records is null)
            throw new ArgumentNullException(nameof(records));

        return records
            .Where(r => r.Status == "PASS" && !string.IsNullOrWhiteSpace(r.ContentHash))
            .GroupBy(r => r.ContentHash!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new Contradiction(
                g.Key,
                g.Select(RecordedDepth).Distinct().OrderBy(d => d).ToArray()))
            .Where(c => c.RecordedDepths.Count > 1)
            .ToArray();
    }

    private static int RecordedDepth(CertificationRecordData record)
    {
        var input = record.Inputs?.FirstOrDefault(i => i.Kind == GenerationLineageInputs.GenerationDepthKind);
        return input is not null && int.TryParse(input.Id, out var depth) ? depth : 0;
    }
}
