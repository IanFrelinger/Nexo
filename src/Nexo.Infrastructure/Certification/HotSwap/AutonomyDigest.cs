using System.Text;

namespace Nexo.Infrastructure.Certification.HotSwap;

/// <summary>
/// The human-reviewable digest (autonomous self-extension spec R7.1/R7.3): Tier-0
/// autonomy never means silence. Renders swap provenance into markdown that keeps two
/// categories visibly distinct — "the loop DID X" (committed swaps, rollbacks,
/// quarantines) versus "the loop PROPOSES X and is HOLDING" (certified-but-held Tier-1
/// artifacts awaiting the human gate, supplied by the caller from the record store) —
/// so held work is presented with evidence, never auto-expired into abandonment.
/// </summary>
public static class AutonomyDigest
{
    /// <summary>One held Tier-1 artifact awaiting human admission (R7.3).</summary>
    public sealed record HeldArtifact(string BrickId, string ContentHash, string Reason);

    /// <summary>Renders the digest for a set of provenance events and held artifacts.</summary>
    public static string Render(
        IReadOnlyList<BrickSwapProvenanceEvent> events,
        IReadOnlyList<HeldArtifact>? held = null)
    {
        if (events is null)
            throw new ArgumentNullException(nameof(events));

        var sb = new StringBuilder();
        sb.AppendLine("# Autonomy digest");
        sb.AppendLine();

        sb.AppendLine("## The loop did");
        sb.AppendLine();
        var did = events
            .Where(e => e.Outcome is BrickSwapProvenanceOutcomes.SwapCommitted
                or BrickSwapProvenanceOutcomes.RollbackCommitted
                or BrickSwapProvenanceOutcomes.WatchBreachQuarantined
                or BrickSwapProvenanceOutcomes.SwapRefused)
            .OrderBy(e => e.Timestamp)
            .ToList();
        if (did.Count == 0)
        {
            sb.AppendLine("- (nothing)");
        }
        else
        {
            foreach (var e in did)
            {
                sb.Append("- ").Append(e.Timestamp.ToString("u"))
                    .Append(" — **").Append(e.Outcome).Append("** generation ").Append(e.Generation);
                if (!string.IsNullOrWhiteSpace(e.Reason))
                    sb.Append(": ").Append(e.Reason);
                sb.AppendLine();
            }
        }

        sb.AppendLine();
        sb.AppendLine("## The loop proposes and is holding");
        sb.AppendLine();
        if (held is null || held.Count == 0)
        {
            sb.AppendLine("- (nothing held)");
        }
        else
        {
            foreach (var artifact in held)
            {
                sb.Append("- **HELD** ").Append(artifact.BrickId)
                    .Append(" (").Append(artifact.ContentHash).Append(") — ")
                    .AppendLine(artifact.Reason);
            }
        }

        return sb.ToString();
    }
}
