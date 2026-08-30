using Ashlar.BackgroundAgents.Forge;
using Ashlar.Core.Application.Paths;

namespace Ashlar.BackgroundAgents.HostRunners;

/// <summary>
/// Applies admitted forge proposals to disk — the APPLY of propose → hold → apply. Called
/// by the admission side only: <c>gates --admit</c> for held proposals, and the bridge for
/// self-extending auto-admissions. Nothing else writes held content.
///
/// <para>This is one of the mediated-write choke points; the governance floor it enforces lives
/// in <see cref="MediatedWritePath"/> so a local self-extend cycle, an imported package and an
/// adopted shared adaptation are all judged by the SAME rules. A mediated write may never touch
/// the project's own contract, its operator-owned policy, anything under <c>.ashlar/</c>, or any
/// build file the receiver's next build would execute — the concrete acts the <c>never</c> list
/// names (modify_gate, widen_sandbox, truncate_ledger, access_signing_keys).</para>
/// </summary>
public static class ForgeApplier
{
    /// <summary>
    /// Applies each proposal's content under <paramref name="repoRoot"/>, with the containment
    /// discipline the rest of the system relies on: a target that escapes the root — lexically OR
    /// through a symlink/junction — that touches a governance path, or (when
    /// <paramref name="writableAllowlist"/> is supplied) that lands outside it, is a hard failure
    /// for the WHOLE batch, validated before any write. A mid-write I/O failure is reported with
    /// exactly which files landed, never as a bare stack trace.
    /// </summary>
    /// <returns>The applied target paths, repo-relative.</returns>
    public static IReadOnlyList<string> ApplyAll(
        ChangeProposalStore store, IReadOnlyList<string> proposalIds, string repoRoot, string actor,
        IReadOnlyList<string>? writableAllowlist = null)
    {
        var rootFull = Path.GetFullPath(repoRoot);

        // Validate every target BEFORE applying any — no partial applies on a bad batch. The floor
        // is the shared MediatedWritePath authority; ForgeApplier only wraps its verdict.
        var resolved = new List<(ChangeProposal Proposal, string FullPath)>();
        foreach (var id in proposalIds)
        {
            var proposal = store.Find(id)
                ?? throw new InvalidOperationException($"Forge proposal '{id}' is not in the store.");
            var refusal = MediatedWritePath.Refuse(rootFull, proposal.TargetPath, writableAllowlist);
            if (refusal is not null)
            {
                throw new InvalidOperationException(
                    $"Forge proposal '{id}' targets {refusal} An admitted brick may never rewrite the "
                    + "envelope that governs it — refusing the whole batch. (never: modify_gate/"
                    + "widen_sandbox/truncate_ledger/access_signing_keys.)");
            }
            var fullPath = Path.GetFullPath(Path.Combine(rootFull, proposal.TargetPath));
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
    /// A project-relative target is a governance path when it is the project contract, the
    /// operator-owned policy, anything under a governance/CI/tooling directory, or a build-executed
    /// file. Delegates to <see cref="MediatedWritePath.IsGovernancePath"/> — the single floor
    /// authority — kept here as the name callers and tests already reach for.
    /// </summary>
    public static bool IsGovernancePath(string relativePath) => MediatedWritePath.IsGovernancePath(relativePath);

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
