using Ashlar.BackgroundAgents.Forge;

namespace Ashlar.BackgroundAgents.HostRunners;

/// <summary>
/// Applies admitted forge proposals to disk — the APPLY of propose → hold → apply. Called
/// by the admission side only: <c>gates --admit</c> for held proposals, and the bridge for
/// self-extending auto-admissions. Nothing else writes held content.
/// </summary>
public static class ForgeApplier
{
    /// <summary>
    /// Applies each proposal's content under <paramref name="repoRoot"/>, with the same
    /// containment discipline as the rest of the system: a target that escapes the root is
    /// a hard failure, never a partial apply.
    /// </summary>
    /// <returns>The applied target paths, repo-relative.</returns>
    public static IReadOnlyList<string> ApplyAll(
        ChangeProposalStore store, IReadOnlyList<string> proposalIds, string repoRoot, string actor)
    {
        var rootFull = Path.GetFullPath(repoRoot);
        var rootWithSep = rootFull.EndsWith(Path.DirectorySeparatorChar)
            ? rootFull
            : rootFull + Path.DirectorySeparatorChar;

        // Validate every target BEFORE applying any — no partial applies on a bad batch.
        var resolved = new List<(ChangeProposal Proposal, string FullPath)>();
        foreach (var id in proposalIds)
        {
            var proposal = store.Find(id)
                ?? throw new InvalidOperationException($"Forge proposal '{id}' is not in the store.");
            var fullPath = Path.GetFullPath(Path.Combine(rootFull, proposal.TargetPath));
            if (!fullPath.StartsWith(rootWithSep, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Forge proposal '{id}' targets '{proposal.TargetPath}', which escapes the project root. Refusing the whole batch.");
            }
            resolved.Add((proposal, fullPath));
        }

        var applied = new List<string>();
        foreach (var (proposal, fullPath) in resolved)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, proposal.NewContent);
            if (proposal.Status == ChangeProposalStatus.Proposed)
            {
                store.Approve(proposal.Id, approver: actor, note: "admitted at the gate");
            }
            store.MarkApplied(proposal.Id, note: $"applied by {actor} via gate admission");
            applied.Add(proposal.TargetPath);
        }
        return applied;
    }

    /// <summary>Rejects held forge proposals — the refuse path, and the gate's automatic
    /// rejection path. The reason is recorded on each proposal.</summary>
    public static void RejectAll(
        ChangeProposalStore store, IReadOnlyList<string> proposalIds, string actor, string reason)
    {
        foreach (var id in proposalIds)
        {
            var proposal = store.Find(id);
            if (proposal is { Status: ChangeProposalStatus.Proposed })
            {
                store.Reject(id, reviewer: actor, note: reason);
            }
        }
    }
}
