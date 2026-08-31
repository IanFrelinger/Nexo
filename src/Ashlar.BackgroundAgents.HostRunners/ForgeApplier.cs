using Ashlar.BackgroundAgents.Forge;
using Ashlar.Core.Application.Certification.Ports;
using Ashlar.Core.Application.Paths;

namespace Ashlar.BackgroundAgents.HostRunners;

/// <summary>
/// The outcome of a verified apply (<see cref="ForgeApplier.ApplyAllWithVerificationAsync"/>): either
/// the writes landed and were committed, or the A4 post-apply canary failed and they were rolled back.
/// </summary>
/// <param name="DidApply">True when the writes passed the canary and were committed to the store.</param>
/// <param name="AppliedPaths">The committed target paths (repo-relative); empty on rollback.</param>
/// <param name="Reason">The canary detail — why it passed, or why it rolled the batch back.</param>
/// <param name="UnrestoredPaths">On a rollback, any files the restore itself could not revert
/// (normally empty). A non-empty list is a LOUD anomaly: the node is in a partially-reverted state.</param>
public sealed record ApplyOutcome(
    bool DidApply,
    IReadOnlyList<string> AppliedPaths,
    string Reason,
    IReadOnlyList<string> UnrestoredPaths)
{
    /// <summary>The canary failed, so the writes were reverted.</summary>
    public bool RolledBack => !DidApply;

    internal static ApplyOutcome ForApplied(IReadOnlyList<string> paths, string reason) =>
        new(true, paths, reason, Array.Empty<string>());

    internal static ApplyOutcome ForRolledBack(string reason, IReadOnlyList<string> unrestored) =>
        new(false, Array.Empty<string>(), reason, unrestored);
}

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
///
/// <para>Applies are transactional on a best-effort basis: every target's prior bytes are snapshotted
/// before any write, so a mid-batch failure — or an A4 post-apply canary rejection — rolls the batch's
/// files back rather than leaving a half-written tree on an unattended node. "Best-effort" is honest:
/// a restore that itself fails (a dying disk, a locked file) is reported LOUDLY via
/// <see cref="ApplyOutcome.UnrestoredPaths"/>, never hidden behind a false success.</para>
/// </summary>
public static class ForgeApplier
{
    /// <summary>
    /// Applies each proposal's content under <paramref name="repoRoot"/>, with the containment
    /// discipline the rest of the system relies on: a target that escapes the root — lexically OR
    /// through a symlink/junction — that touches a governance path, or (when
    /// <paramref name="writableAllowlist"/> is supplied) that lands outside it, is a hard failure
    /// for the WHOLE batch, validated before any write. A mid-write I/O failure rolls back every
    /// file already written in the batch, so the tree is never left half-applied.
    /// </summary>
    /// <returns>The applied target paths, repo-relative.</returns>
    public static IReadOnlyList<string> ApplyAll(
        ChangeProposalStore store, IReadOnlyList<string> proposalIds, string repoRoot, string actor,
        IReadOnlyList<string>? writableAllowlist = null)
    {
        var rootFull = Path.GetFullPath(repoRoot);
        var (resolved, _) = StageWrites(store, proposalIds, rootFull, writableAllowlist);
        CommitApplied(store, resolved, actor);
        return resolved.Select(r => r.Proposal.TargetPath).ToList();
    }

    /// <summary>
    /// Applies the batch, then runs the A4 post-apply canary over the files as they landed, and only
    /// commits the admission if the canary passes; on a canary failure (or verifier error — fail-closed)
    /// every write is rolled back and the proposals are rejected with the canary's reason. This is the
    /// safety net the unattended auto-apply posture rests on: an auto-admitted change that does not
    /// survive verification never stays on the node.
    /// </summary>
    public static async Task<ApplyOutcome> ApplyAllWithVerificationAsync(
        ChangeProposalStore store, IReadOnlyList<string> proposalIds, string repoRoot, string actor,
        IPostApplyVerification verification, IReadOnlyList<string>? writableAllowlist = null,
        CancellationToken ct = default)
    {
        var rootFull = Path.GetFullPath(repoRoot);
        var (resolved, snapshot) = StageWrites(store, proposalIds, rootFull, writableAllowlist);

        var appliedFiles = resolved
            .Select(r => new AppliedFile(r.Proposal.TargetPath, r.FullPath))
            .ToList();

        PostApplyVerificationResult? result;
        try
        {
            result = await verification.VerifyAsync(rootFull, appliedFiles, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Fail-closed: a canary that cannot decide must not let an unattended change survive.
            result = new PostApplyVerificationResult(false, $"canary errored: {ex.Message}");
        }

        // Fail-closed even for a MISBEHAVING verifier that returns a null/absent verdict rather than
        // throwing: "no decision" may never let an unattended change stay on disk.
        result ??= new PostApplyVerificationResult(false, "canary returned no result");

        if (result.Passed)
        {
            CommitApplied(store, resolved, actor);
            return ApplyOutcome.ForApplied(resolved.Select(r => r.Proposal.TargetPath).ToList(), result.Detail);
        }

        var failedRestore = snapshot.Restore();
        RejectAll(store, proposalIds, actor, $"post-apply canary failed: {result.Detail}");
        return ApplyOutcome.ForRolledBack(result.Detail, failedRestore);
    }

    /// <summary>
    /// Validates the whole batch, snapshots every target's prior state, then writes them all. A write
    /// failure mid-batch restores the snapshot and throws — nothing is left half-applied. Returns the
    /// resolved proposals and the snapshot so the caller can roll back after a later verification step.
    /// The store is NOT marked here: callers commit (or reject) explicitly once the outcome is known.
    /// </summary>
    private static (List<(ChangeProposal Proposal, string FullPath)> Resolved, TargetSnapshot Snapshot) StageWrites(
        ChangeProposalStore store, IReadOnlyList<string> proposalIds, string rootFull,
        IReadOnlyList<string>? writableAllowlist)
    {
        // Validate every target BEFORE applying any — no partial applies on a bad batch. The floor
        // is the shared MediatedWritePath authority; ForgeApplier only wraps its verdict. Ids are
        // de-duplicated (a repeated id would otherwise drive the same row through the status machine
        // twice and throw on the second pass) while preserving order.
        var resolved = new List<(ChangeProposal Proposal, string FullPath)>();
        foreach (var id in proposalIds.Distinct())
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

        // Snapshot prior state so ANY failure past this point can be rolled back. If the prior state
        // cannot even be read, rollback could not be promised, so refuse before touching anything.
        TargetSnapshot snapshot;
        try
        {
            snapshot = TargetSnapshot.Capture(resolved.Select(r => r.FullPath));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Cannot snapshot targets for rollback ({ex.Message}); refusing to apply — nothing was written.", ex);
        }

        ChangeProposal? failingAt = null;
        try
        {
            foreach (var (proposal, fullPath) in resolved)
            {
                failingAt = proposal;
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                File.WriteAllText(fullPath, proposal.NewContent);
            }
        }
        catch (Exception ex)
        {
            // ANY failure past the first write — a target that is locked, read-only, already a
            // directory or a reserved name (I/O, access) OR an unforeseen provider fault — rolls the
            // whole batch back rather than leaving a half-written tree on an unattended node.
            var failedRestore = snapshot.Restore();
            var state = failedRestore.Count == 0
                ? "All writes in this batch were rolled back; no files changed."
                : $"ROLLBACK INCOMPLETE — these files could not be restored and remain changed: {string.Join(", ", failedRestore)}.";
            throw new InvalidOperationException(
                $"Apply failed writing '{failingAt?.TargetPath}' ({ex.Message}). {state}"
                + " The admission is recorded; fix the target and re-drive the apply.", ex);
        }

        return (resolved, snapshot);
    }

    /// <summary>Marks the resolved proposals applied in the store — the commit half of an apply.</summary>
    private static void CommitApplied(
        ChangeProposalStore store, List<(ChangeProposal Proposal, string FullPath)> resolved, string actor)
    {
        foreach (var (proposal, _) in resolved)
        {
            // Re-read the live status, and be idempotent: a row already applied is left alone rather
            // than driven through the status machine again (which would throw on the second pass).
            var current = store.Find(proposal.Id) ?? proposal;
            if (current.Status == ChangeProposalStatus.Applied)
            {
                continue;
            }
            if (current.Status == ChangeProposalStatus.Proposed)
            {
                store.Approve(proposal.Id, approver: actor, note: "admitted at the gate");
            }
            store.MarkApplied(proposal.Id, note: $"applied by {actor} via gate admission");
        }
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

    /// <summary>
    /// Prior-state capture for a known set of targets, so an apply can be rolled back to exactly where
    /// it started. Scoped to the batch's own paths — lighter and more precise than a workspace-wide
    /// snapshot store, which is the wrong granularity for "revert exactly these N files."
    /// </summary>
    private sealed class TargetSnapshot
    {
        private readonly List<(string FullPath, byte[]? PriorBytes, bool Existed)> _entries;

        private TargetSnapshot(List<(string, byte[]?, bool)> entries) => _entries = entries;

        public static TargetSnapshot Capture(IEnumerable<string> fullPaths)
        {
            var entries = new List<(string, byte[]?, bool)>();
            foreach (var fp in fullPaths)
            {
                if (File.Exists(fp))
                {
                    entries.Add((fp, File.ReadAllBytes(fp), true));
                }
                else
                {
                    entries.Add((fp, null, false));
                }
            }
            return new TargetSnapshot(entries);
        }

        /// <summary>
        /// Restores every target FILE to its captured state — rewriting prior bytes, or deleting a
        /// file that did not exist before. (A directory newly created to hold a now-deleted file may
        /// remain, empty and harmless; only file content is reverted.) Best-effort across all entries:
        /// it attempts every one and returns the full paths it could NOT restore, so the caller can
        /// report a partial rollback loudly rather than falsely claim success.
        /// </summary>
        public IReadOnlyList<string> Restore()
        {
            var failed = new List<string>();
            foreach (var (fullPath, priorBytes, existed) in _entries)
            {
                try
                {
                    if (existed)
                    {
                        File.WriteAllBytes(fullPath, priorBytes!);
                    }
                    else if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    failed.Add(fullPath);
                }
            }
            return failed;
        }
    }
}
