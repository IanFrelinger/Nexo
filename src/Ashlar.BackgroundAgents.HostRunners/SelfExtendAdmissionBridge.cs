using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Certification.Ports;
using Ashlar.Manifest;
using Ashlar.Manifest.Admission;
using Ashlar.Manifest.Packaging;
using Ashlar.Manifest.Signing;

namespace Ashlar.BackgroundAgents.HostRunners;

/// <summary>
/// The producer wiring: when a self-extend cycle runs inside an ashlar project (a repo with
/// <c>ashlar.policy.yaml</c> at its root), the cycle's outcome is recorded as an
/// <see cref="ExtensionProposal"/> through the admission gate — the same store
/// <c>ashlar gates</c> reads, so a cycle's work shows up in the held queue for a person to
/// seat or refuse.
///
/// <para>Outside an ashlar project this is a no-op: the runner behaves exactly as before.
/// The courses attached to the proposal are ONLY what the cycle actually evidences — today
/// that is the <c>sandbox</c> course (from the policy engine's denial count). A project
/// whose policy requires more gates (tests, security) will therefore see the proposal
/// REJECTED with "gate did not run" — which is correct and fail-closed: the runtime may not
/// claim courses it did not run, and the operator's policy decides whether the evidence
/// suffices.</para>
///
/// <para>Honest scope note: v0 records AFTER the cycle's mediated writes, making the gate a
/// ledger of what happened plus the human queue for it. Moving admission BEFORE the write
/// lands (propose → hold → apply) is the M1 enforcement ordering, tracked in SPEC-004.</para>
/// </summary>
public static class SelfExtendAdmissionBridge
{
    /// <summary>
    /// Records the cycle as a proposal when running inside an ashlar project.
    /// </summary>
    /// <returns>A one-line gate outcome for the run summary, or null when not an ashlar
    /// project or nothing was written.</returns>
    /// <remarks><paramref name="autoShare"/> opts an ADMITTED cycle into sharing its sealed
    /// package to the mesh (null defaults from <c>ASHLAR_MESH_AUTOSHARE=1</c>);
    /// <paramref name="meshDir"/> overrides the store (null resolves via
    /// <see cref="MeshStore.Resolve"/>). Tests inject both; they never mutate env.</remarks>
    public static async Task<string?> TryRecordAsync(
        string repoRoot,
        string agentName,
        string? objective,
        IReadOnlyList<string> writePaths,
        int toolCallsExecuted,
        int toolCallsDenied,
        ILogger logger,
        CancellationToken ct = default,
        IReadOnlyList<string>? forgeProposalIds = null,
        SigningIdentity? signer = null,
        bool? autoShare = null,
        string? meshDir = null,
        IExtensionCompileCheck? compileCheck = null)
    {
        var policyPath = Path.Combine(repoRoot, "ashlar.policy.yaml");
        if (!File.Exists(policyPath))
        {
            return null;   // not an ashlar project; the runner is unchanged
        }
        forgeProposalIds ??= [];
        if (writePaths.Count == 0 && forgeProposalIds.Count == 0)
        {
            return null;   // nothing to propose
        }

        string policyYaml;
        try
        {
            policyYaml = await File.ReadAllTextAsync(policyPath, ct).ConfigureAwait(false);
        }
        catch (IOException ex)
        {
            logger.LogError(ex, "Self-extend gate: could not read ashlar.policy.yaml");
            return $"GATE ERROR: could not read ashlar.policy.yaml ({ex.Message})";
        }

        if (!PolicyLoader.TryLoad(policyYaml, out var policy, out var reason))
        {
            // Fail loud, never silent: an unreadable envelope is an error the operator must
            // see, not a skipped gate.
            logger.LogError("Self-extend gate: policy rejected: {Reason}", reason);
            return $"GATE ERROR: {reason}";
        }

        var forge = forgeProposalIds.Count > 0 ? AshlarProjectMediation.ProjectStore(repoRoot) : null;

        // A2 — executed evidence. Compile the proposed source (Roslyn, in-process) so the gate
        // admits on RUN evidence, not a self-reported claim. A change that does not compile earns a
        // FAILED build course and is rejected by any policy that requires `build`. Fail-closed: a
        // verifier error is a failed course, never a pass. Skipped only when no compile check is
        // wired or there is nothing mediated to compile (behaviour then matches pre-A2 exactly).
        CourseResult? buildCourse = null;
        if (compileCheck is not null && forge is not null && forgeProposalIds.Count > 0)
        {
            var proposedFiles = new List<ProposedFileContent>(forgeProposalIds.Count);
            foreach (var id in forgeProposalIds)
            {
                var row = forge.Find(id);
                if (row is not null)
                {
                    proposedFiles.Add(new ProposedFileContent(row.TargetPath, row.NewContent));
                }
            }
            try
            {
                var result = await compileCheck.CheckAsync(proposedFiles, ct).ConfigureAwait(false);
                buildCourse = new CourseResult { Name = "build", Passed = result.Passed, Detail = result.Detail };
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Self-extend gate: build compile-check errored");
                buildCourse = new CourseResult { Name = "build", Passed = false, Detail = $"compile check errored: {ex.Message}" };
            }
        }

        ExtensionProposal proposal;
        try
        {
            proposal = BuildProposal(agentName, objective, writePaths, toolCallsExecuted, toolCallsDenied,
                forgeProposalIds, forge, buildCourse is null ? null : new[] { buildCourse });
        }
        catch (InvalidOperationException ex)
        {
            // A cycle that references a row the store does not hold cannot claim its content, and
            // a record signed without the claim is unrepairable (append-once). Same loud GATE
            // ERROR shape as an unreadable policy — never a silently under-claimed record.
            logger.LogError(ex, "Self-extend gate: cycle references a forge row that is not in the store");
            return $"GATE ERROR: {ex.Message}";
        }

        // Sign the runtime's own proposals with the operator identity, when one is present, so a
        // self-extend cycle's recorded verdict carries the same provenance as an operator's manual
        // decision (SPEC-006). Tests inject the identity; production loads the machine key. A
        // corrupt key fails the record LOUD — the same GATE ERROR shape as an unreadable policy —
        // rather than silently recording unsigned, which would hide that signing was ever expected.
        if (signer is null)
        {
            try
            {
                signer = OperatorKey.TryLoad();
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex, "Self-extend gate: operator key is corrupt");
                return $"GATE ERROR: {ex.Message}";
            }
        }

        var store = new GateStore(Path.Combine(repoRoot, ".ashlar"), signer);
        var record = await store.ProposeAsync(policy!, proposal, DateTimeOffset.UtcNow, ct).ConfigureAwait(false);

        logger.LogInformation(
            "Self-extend gate: proposal {Id} -> {State} ({Reason})",
            proposal.Id, record.State, record.Reason);

        // M1 propose → hold → APPLY: the gate's verdict decides what happens to the parked
        // forge proposals. Held leaves them parked for `gates --admit`; a rejection rejects
        // them (sealed thereby truly seals — nothing was on disk, nothing ever lands); an
        // automatic self-extending admission applies them now, within budget, as decided.
        var shareNote = string.Empty;
        if (forge is not null)
        {
            switch (record.State)
            {
                case ProposalState.Rejected:
                    ForgeApplier.RejectAll(forge, forgeProposalIds, "gate", record.Reason);
                    break;
                case ProposalState.Admitted:
                    var applied = ForgeApplier.ApplyAll(forge, forgeProposalIds, repoRoot, "gate",
                        policy!.Sandbox.EnforceWritableAllowlist ? policy.Sandbox.Writable : null);
                    logger.LogInformation("Self-extend gate: applied {Count} mediated write(s)", applied.Count);
                    shareNote = TryAutoShare(record, forge, forgeProposalIds, signer, autoShare, meshDir, logger);
                    break;
            }
        }

        return record.State switch
        {
            ProposalState.Held => $"GATE: held as {proposal.Id} — review with `ashlar gates`",
            ProposalState.Admitted => $"GATE: admitted as {proposal.Id} — {record.Reason}{shareNote}",
            _ => $"GATE: rejected — {record.Reason}",
        };
    }

    /// <summary>
    /// Best-effort auto-share of an admitted, applied extension into the mesh (co-production:
    /// peers pull what this cycle produced, through THEIR OWN gates). Opt-in — the autoShare
    /// parameter, or <c>ASHLAR_MESH_AUTOSHARE=1</c> when it is null. Never fails the cycle: a
    /// share failure logs a warning and annotates the outcome string, nothing more. The
    /// admission and its applied writes stand regardless.
    /// </summary>
    internal static string TryAutoShare(
        GateRecord record,
        Ashlar.BackgroundAgents.Forge.ChangeProposalStore forge,
        IReadOnlyList<string> forgeProposalIds,
        SigningIdentity? signer,
        bool? autoShare,
        string? meshDir,
        ILogger logger)
    {
        var enabled = autoShare ?? Environment.GetEnvironmentVariable("ASHLAR_MESH_AUTOSHARE") == "1";
        if (!enabled)
        {
            return string.Empty;
        }

        // A package's seal is a signature (SPEC-006): an unsigned admission cannot travel.
        // Skipping is honest and non-fatal — the annotation says why nothing reached the mesh.
        if (signer is null || record.Sig is null || record.Signer is null)
        {
            logger.LogWarning("Self-extend gate: auto-share skipped — the admission is unsigned (run `ashlar keys init`)");
            return "; auto-share skipped: unsigned admission (run `ashlar keys init`)";
        }

        try
        {
            var files = new List<PackageFile>();
            foreach (var id in forgeProposalIds)
            {
                var proposal = forge.Find(id)
                    ?? throw new InvalidOperationException($"forge proposal '{id}' is missing — cannot package an incomplete extension.");
                // ApplyAll just marked these rows Applied. Anything else under this id is a row
                // the gate did not admit-and-apply (a shadow, a replacement) — refuse to seal it.
                if (proposal.Status != Ashlar.BackgroundAgents.Forge.ChangeProposalStatus.Applied)
                {
                    throw new InvalidOperationException(
                        $"forge proposal '{id}' is {proposal.Status}, not Applied — only content the gate admitted AND applied may travel.");
                }
                files.Add(new PackageFile { Path = proposal.TargetPath, Content = proposal.NewContent });
            }
            // The rows just re-read from the mutable forge store must hash to the claims the
            // signed admission carries — a row edited since the gate decided must not travel
            // under the origin's signature. Refusing is still best-effort: the admission and
            // its applied writes stand, only the share is withheld.
            if (!ExtensionPackaging.VerifyFileClaims(record.Proposal, files, out var claimReason))
            {
                logger.LogWarning("Self-extend gate: auto-share refused — {Reason}", claimReason);
                return $"; auto-share refused: {claimReason}";
            }
            var json = ExtensionPackaging.Pack(record, files, signer);
            var dest = MeshStore.Publish(MeshStore.Resolve(meshDir), json);
            logger.LogInformation("Self-extend gate: shared to the mesh → {Dest}", dest);
            return $"; shared → {dest}";
        }
        catch (Exception ex)
        {
            // Best-effort means BEST-EFFORT: no exception class thrown in here may destroy a
            // successful cycle — an escape would replace the whole run result with a failure.
            logger.LogWarning(ex, "Self-extend gate: auto-share failed — the admission stands, the share did not happen");
            return $"; auto-share failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Maps cycle facts onto a proposal. Public and pure-over-the-store so tests pin the
    /// mapping. The courses claim ONLY what the cycle evidences: the sandbox course from the
    /// policy engine's own denial count. When a store is supplied, each referenced row is
    /// resolved exactly once and yields BOTH its diff line and its content claim (path,
    /// sha256(NewContent)) — the claims are signed with the record, so packaging can later
    /// prove the rows it re-reads are the bytes this gate decided over. A referenced row
    /// missing from the store throws: the claim it would sign covers nothing, the record is
    /// append-once, and a silently under-claimed admission could never travel — refusing loud
    /// at propose is the only repairable shape.
    /// </summary>
    public static ExtensionProposal BuildProposal(
        string agentName,
        string? objective,
        IReadOnlyList<string> writePaths,
        int toolCallsExecuted,
        int toolCallsDenied,
        IReadOnlyList<string>? forgeProposalIds = null,
        Ashlar.BackgroundAgents.Forge.ChangeProposalStore? forge = null,
        IReadOnlyList<CourseResult>? extraCourses = null)
    {
        forgeProposalIds ??= [];
        var mediated = forgeProposalIds.Count > 0;

        List<Ashlar.BackgroundAgents.Forge.ChangeProposal>? rows = null;
        if (mediated && forge is not null)
        {
            rows = new List<Ashlar.BackgroundAgents.Forge.ChangeProposal>(forgeProposalIds.Count);
            foreach (var id in forgeProposalIds)
            {
                rows.Add(forge.Find(id) ?? throw new InvalidOperationException(
                    $"forge proposal '{id}' is not in the store — park the write before proposing; "
                    + "a record cannot claim content the store does not hold."));
            }
        }

        // Mediated: the sandbox claim is STRUCTURAL — every write is a parked proposal and
        // nothing touched disk, so confinement holds by construction regardless of denial
        // count (mediation denials are steering, not violations). Unmediated: the claim
        // rests on the policy engine's denial count, as before.
        var confined = mediated || toolCallsDenied == 0;
        var sandboxDetail = mediated
            ? $"writes mediated: {forgeProposalIds.Count} proposal(s) parked, nothing touched disk"
            : confined
                ? $"{toolCallsExecuted} tool call(s), 0 denied"
                : $"{toolCallsDenied} tool call(s) DENIED by the policy engine";

        var diffLines = rows is not null
            ? rows.Select(p => $"~ {p.TargetPath}  ({Truncate(p.Summary, 60)})")
            : writePaths.Select(p => "~ " + p);

        return new ExtensionProposal
        {
            // Matches the store's id allowlist: alphanumeric start, [A-Za-z0-9_-].
            Id = "ext-" + Guid.NewGuid().ToString("N")[..12],
            Kind = "brick",
            Summary = string.IsNullOrWhiteSpace(objective)
                ? $"self-extend cycle by {agentName}: {(mediated ? forgeProposalIds.Count : writePaths.Count)} change(s)"
                : Truncate(objective!, 120),
            ProposedBy = agentName,
            ProposedAt = DateTimeOffset.UtcNow,
            Diff = string.Join("\n", diffLines),
            ForgeProposalIds = forgeProposalIds,
            Files = rows?.Select(p => FileClaim.For(p.TargetPath, p.NewContent)).ToList(),
            Courses = BuildCourses(confined, sandboxDetail, extraCourses),
        };
    }

    private static List<CourseResult> BuildCourses(
        bool confined, string sandboxDetail, IReadOnlyList<CourseResult>? extraCourses)
    {
        var courses = new List<CourseResult>
        {
            new CourseResult { Name = "sandbox", Passed = confined, Detail = sandboxDetail },
        };
        if (extraCourses is not null)
        {
            courses.AddRange(extraCourses);
        }
        return courses;
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..(max - 1)] + "…";
}
