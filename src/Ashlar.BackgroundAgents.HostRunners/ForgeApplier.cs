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
        ChangeProposalStore store, IReadOnlyList<string> proposalIds, string repoRoot, string actor,
        IReadOnlyList<string>? writableAllowlist = null)
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

            // Governance is checked on the NORMALIZED, root-relative form — never the raw target.
            // Path.GetFullPath above already collapsed '.', '..' and mixed separators, so
            // './ashlar.policy.yaml', 'a/../.ashlar/x' and '.\\ashlar.yaml' all reduce to the same
            // canonical path the write will actually hit. Checking the raw string let every one of
            // those spellings slip past a denylist keyed on the first segment.
            var normalizedRel = Path.GetRelativePath(rootFull, fullPath).Replace('\\', '/');
            if (IsGovernancePath(normalizedRel))
            {
                throw new InvalidOperationException(
                    $"Forge proposal '{id}' targets '{target}' (resolves to '{normalizedRel}'), a governance or "
                    + "build-integrity path. An admitted brick may never rewrite the envelope that governs it, nor a "
                    + "build file the receiver's next `dotnet build` would execute — refusing the whole batch. "
                    + "(never: modify_gate/widen_sandbox/truncate_ledger/access_signing_keys.)");
            }
            if (writableAllowlist is { Count: > 0 } && !IsUnderWritableAllowlist(normalizedRel, writableAllowlist))
            {
                throw new InvalidOperationException(
                    $"Forge proposal '{id}' targets '{target}' (resolves to '{normalizedRel}'), outside the policy's "
                    + "writable allowlist. This project set sandbox.enforceWritableAllowlist, so a mediated write must "
                    + "land under one of sandbox.writable — refusing the whole batch.");
            }
            if (TraversesReparsePoint(fullPath, rootFull) || PathIsReparsePoint(fullPath))
            {
                throw new InvalidOperationException(
                    $"Forge proposal '{id}' targets '{target}', whose path runs through — or ends at — a symlink or "
                    + "junction that could leave the project root. Lexical containment is not enough when a link is in "
                    + "the way, and a File.WriteAllText through a leaf symlink truncates the link's target — refusing "
                    + "the whole batch.");
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
        if (segments.Length == 0)
        {
            return false;
        }

        // Directory prefixes: everything beneath these is governance state, version control, CI,
        // editor/devcontainer config, or the project's own scripts — none of which a mediated
        // write may touch. Keyed on the FIRST segment of the normalized path.
        foreach (var dir in GovernanceDirPrefixes)
        {
            if (string.Equals(segments[0], dir, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Files dangerous at ANY depth. MSBuild, NuGet, the .NET SDK, make, pre-commit and the
        // analyzer config are all discovered by walking the tree (or sit at a well-known name), so
        // a governed/executed file dropped anywhere below the root runs or is honoured on the
        // receiver's next build/commit — code execution or a silenced control outside the loader,
        // the gate and the registry entirely. The project contract and the operator policy are
        // denied at any depth for the same reason: a brick has no business authoring either name.
        var fileName = segments[^1];
        foreach (var name in GovernanceFileNames)
        {
            if (string.Equals(fileName, name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Suffix families, any depth. MSBuild imports Directory.Build.*, Directory.Packages.props,
        // Directory.Solution.*, before./after.<sln>.sln.targets AND any custom-<Import>ed file —
        // they all end in .props/.targets, so the whole family is denied rather than an enumerated
        // list an attacker can step around (the write-floor's original miss: Directory.Solution.targets).
        // Project and solution files carry <Target>s, analyzers and PackageReferences that run at
        // build time; .slnx is the new XML solution format.
        foreach (var suffix in GovernanceFileSuffixes)
        {
            if (fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static readonly string[] GovernanceDirPrefixes =
    {
        ".ashlar", ".git", ".github", ".vscode", ".devcontainer", "scripts",
    };

    private static readonly string[] GovernanceFileNames =
    {
        "ashlar.yaml", "ashlar.policy.yaml",
        "nuget.config", "global.json",
        "Makefile", "GNUmakefile",           // GNU make reads GNUmakefile before Makefile; OIC covers makefile/MAKEFILE
        ".editorconfig",                      // a nested one can set analyzer severity=none, silencing the repo's own gates
        ".pre-commit-config.yaml",            // a `repo: local` hook runs on the next git commit
    };

    private static readonly string[] GovernanceFileSuffixes =
    {
        ".props", ".targets",                                   // every MSBuild import, incl. Directory.Solution.*
        ".csproj", ".fsproj", ".vbproj", ".proj", ".sln", ".slnx",
    };

    /// <summary>
    /// True when <paramref name="normalizedRel"/> (forward-slash, root-relative) is an allowlist
    /// entry itself or sits beneath one. Entries are normalized the same way so 'src', 'src/' and
    /// 'src\\x' compare alike.
    /// </summary>
    private static bool IsUnderWritableAllowlist(string normalizedRel, IReadOnlyList<string> allowlist)
    {
        foreach (var entry in allowlist)
        {
            var e = entry.Replace('\\', '/').Trim('/');
            if (e.Length == 0)
            {
                continue;
            }
            if (string.Equals(normalizedRel, e, StringComparison.OrdinalIgnoreCase)
                || normalizedRel.StartsWith(e + "/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
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
    /// <summary>
    /// True when the target path itself already exists and is a symlink/junction. The ancestor
    /// walk in <see cref="TraversesReparsePoint"/> stops at the parent, so without this a
    /// pre-planted leaf link (e.g. docs/site.yaml -> ../ashlar.policy.yaml) would be followed by
    /// File.WriteAllText and truncate the link's target — a write onto a governance path that the
    /// lexical, link-blind normalization cannot see.
    /// </summary>
    private static bool PathIsReparsePoint(string fullPath)
    {
        if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
        {
            return false;
        }
        return (File.GetAttributes(fullPath) & FileAttributes.ReparsePoint) != 0;
    }

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
