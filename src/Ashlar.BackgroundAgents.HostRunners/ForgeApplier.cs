using Ashlar.BackgroundAgents.Forge;

namespace Ashlar.BackgroundAgents.HostRunners;

/// <summary>
/// Applies admitted forge proposals to disk — the APPLY of propose → hold → apply. Called
/// by the admission side only: <c>gates --admit</c> for held proposals, and the bridge for
/// self-extending auto-admissions. Nothing else writes held content.
///
/// <para>This is the single choke point for every mediated write — a local self-extend cycle
/// and an imported package both land here — so the self-governance guarantees are enforced
/// HERE, structurally, not merely declared in the policy for humans to read. A mediated write
/// may never touch the project's own contract, its operator-owned policy, or anything under
/// <c>.ashlar/</c> (the gate records, the signed ledger, the forge queue, any project-local
/// key material). Those are the concrete acts the <c>never</c> list names — modify_gate,
/// widen_sandbox, truncate_ledger, access_signing_keys — and admission of a brick must not be
/// able to perform them. An imported package that admits under a self-extending policy could
/// otherwise rewrite the very envelope that governs it.</para>
/// </summary>
public static class ForgeApplier
{
    /// <summary>
    /// Applies each proposal's content under <paramref name="repoRoot"/>, with the containment
    /// discipline the rest of the system relies on: a target that escapes the root — lexically OR
    /// through a symlink/junction — or that touches a governance path is a hard failure for the
    /// WHOLE batch, validated before any write. A mid-write I/O failure is reported with exactly
    /// which files landed, never as a bare stack trace.
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
            var target = proposal.TargetPath;
            var fullPath = Path.GetFullPath(Path.Combine(rootFull, target));

            if (!fullPath.StartsWith(rootWithSep, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Forge proposal '{id}' targets '{target}', which escapes the project root. Refusing the whole batch.");
            }
            if (IsGovernancePath(target))
            {
                throw new InvalidOperationException(
                    $"Forge proposal '{id}' targets '{target}', a governance path (the project contract, the "
                    + "operator-owned policy, or .ashlar/ state). An admitted brick may never rewrite the "
                    + "envelope that governs it — refusing the whole batch. (never: modify_gate/widen_sandbox/"
                    + "truncate_ledger/access_signing_keys.)");
            }
            if (TraversesReparsePoint(fullPath, rootFull))
            {
                throw new InvalidOperationException(
                    $"Forge proposal '{id}' targets '{target}', whose path runs through a symlink or junction "
                    + "that could leave the project root. Lexical containment is not enough when a link is in the "
                    + "way — refusing the whole batch.");
            }
            resolved.Add((proposal, fullPath));
        }

        var applied = new List<string>();
        foreach (var (proposal, fullPath) in resolved)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                File.WriteAllText(fullPath, proposal.NewContent);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A target that is locked, read-only, already a directory, or a reserved device
                // name fails mid-batch. Rollback of already-written files is not something this
                // step can promise, so instead be exact about the durable state — never a bare
                // stack trace that leaves the operator guessing which files landed.
                throw new InvalidOperationException(
                    $"Apply failed writing '{proposal.TargetPath}' ({ex.Message}). "
                    + (applied.Count == 0
                        ? "No files were written."
                        : $"These files WERE written and remain on disk: {string.Join(", ", applied)}.")
                    + " The admission is recorded; fix the target and the remaining writes must be re-driven manually.");
            }
            if (proposal.Status == ChangeProposalStatus.Proposed)
            {
                store.Approve(proposal.Id, approver: actor, note: "admitted at the gate");
            }
            store.MarkApplied(proposal.Id, note: $"applied by {actor} via gate admission");
            applied.Add(proposal.TargetPath);
        }
        return applied;
    }

    /// <summary>
    /// A project-relative target is a governance path when it is the project contract
    /// (<c>ashlar.yaml</c>), the operator-owned policy (<c>ashlar.policy.yaml</c>), or anything
    /// under <c>.ashlar/</c>. Comparison is case-insensitive because the file systems this runs on
    /// are, and an admitted write must not reach these under any spelling.
    /// </summary>
    public static bool IsGovernancePath(string relativePath)
    {
        var segments = relativePath.Split('/', '\\');
        if (segments.Length > 0 && string.Equals(segments[0], ".ashlar", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        if (segments.Length == 1)
        {
            return string.Equals(segments[0], "ashlar.yaml", StringComparison.OrdinalIgnoreCase)
                || string.Equals(segments[0], "ashlar.policy.yaml", StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    /// <summary>
    /// True when any existing directory on the path from the root down to the target's parent is a
    /// reparse point (symlink/junction). <see cref="Path.GetFullPath(string)"/> normalizes
    /// <c>..</c> lexically but does NOT follow links, so a junction ancestor could carry a
    /// lexically-in-root write to a real location outside the root. A governed write never
    /// traverses a link, so the presence of one on the path fails the batch.
    /// </summary>
    private static bool TraversesReparsePoint(string targetFullPath, string rootFull)
    {
        var dir = Path.GetDirectoryName(targetFullPath);
        while (dir is not null && dir.Length > rootFull.Length && dir.StartsWith(rootFull, StringComparison.Ordinal))
        {
            if (Directory.Exists(dir) && (File.GetAttributes(dir) & FileAttributes.ReparsePoint) != 0)
            {
                return true;
            }
            dir = Path.GetDirectoryName(dir);
        }
        return false;
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
