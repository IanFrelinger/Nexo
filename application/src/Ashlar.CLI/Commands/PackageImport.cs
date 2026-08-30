using Ashlar.BackgroundAgents.Forge;
using Ashlar.BackgroundAgents.HostRunners;
using Ashlar.Manifest;
using Ashlar.Manifest.Admission;
using Ashlar.Manifest.Packaging;
using Ashlar.Manifest.Signing;

namespace Ashlar.CLI.Commands;

/// <summary>How a submitted package fared at the receiving project's gate.</summary>
public enum PackageAdmission
{
    /// <summary>The package failed intrinsic verification, or the project's policy would not load —
    /// nothing was proposed.</summary>
    Refused,

    /// <summary>Verified, admitted, and applied to the project tree.</summary>
    Admitted,

    /// <summary>Verified and held for a human decision — nothing on disk.</summary>
    Held,

    /// <summary>Verified but rejected by the gate (e.g. sealed, over budget, outside the
    /// envelope) — nothing on disk.</summary>
    Rejected,

    /// <summary>Its verdict id is already recorded at this gate — a re-pull of something already
    /// decided here. Nothing is parked or changed (append-once history is not re-opened).</summary>
    AlreadyImported,
}

/// <summary>The outcome of submitting one package, transport-agnostic. <see cref="Warning"/> is
/// set when the admission was recorded but the apply did not fully complete — a durable,
/// operator-visible fact that a green "admitted" line must not hide.</summary>
public sealed record PackageImportResult(
    PackageAdmission Outcome,
    string Message,
    ExtensionPackage? Package,
    string? LocalProposalId,
    IReadOnlyList<string> AppliedPaths,
    string? Warning = null);

/// <summary>
/// The receiver-sovereign import: verify a certified package INTRINSICALLY (no local keys, no
/// network), then submit it to THIS project's gate under THIS project's policy. Shared by
/// <c>ashlar pkg import</c> (one file) and <c>ashlar mesh pull</c> (many from a peer), because
/// how a package arrived must never change how it is admitted — the transport is not the trust.
///
/// <para>Never throws for an expected outcome: a forged package, a full budget, a sealed project,
/// or an already-imported id all come back as a <see cref="PackageImportResult"/> so a batch
/// puller can report each and continue. Origin certification travels as EVIDENCE; the local gate
/// is the AUTHORITY.</para>
/// </summary>
public static class PackageImport
{
    public static async Task<PackageImportResult> SubmitAsync(string projectDir, string packageJson)
    {
        if (!ExtensionPackaging.TryOpen(packageJson, out var pkg, out var reason))
        {
            return new(PackageAdmission.Refused, reason, null, null, []);
        }

        var policyPath = Path.Combine(projectDir, "ashlar.policy.yaml");
        if (!File.Exists(policyPath))
        {
            return new(PackageAdmission.Refused, "not an ashlar project (no ashlar.policy.yaml)", pkg, null, []);
        }
        if (!PolicyLoader.TryLoad(await File.ReadAllTextAsync(policyPath), out var policy, out var policyReason))
        {
            return new(PackageAdmission.Refused, policyReason, pkg, null, []);
        }

        // TRUST ROOT (Phase 3): a package sealed by a key this project does not trust never parks
        // anything — the refusal comes BEFORE the forge queue, so a stranger cannot fill the disk by
        // spamming pulls, and `× REFUSED` names the fingerprint to trust. The check is the sealer's
        // fingerprint against the project policy's trustedSigners UNION the operator's local peers
        // keychain (`ashlar keys trust <fp>`). Empty trust set ⇒ every import refused (fail-closed).
        var sealer = Fingerprint(pkg!.SealSigner);
        if (!OperatorKey.IsSignerTrusted(sealer, policy!.SelfExtend.TrustedSigners))
        {
            return new(PackageAdmission.Refused,
                $"sealed by {sealer}, which is not a trusted signer. Read the fingerprint off the origin "
                + "box's `ashlar keys show`, then `ashlar keys trust " + sealer + "` here — or list it under "
                + "selfExtend.trustedSigners in ashlar.policy.yaml.",
                pkg, null, []);
        }

        var forge = AshlarProjectMediation.ProjectStore(projectDir);
        var store = new GateStore(Path.Combine(projectDir, ".ashlar"), OperatorKey.TryLoad());
        // Declared out here so the catch can clean up anything parked before a failure — otherwise
        // a routine re-pull (which parks fresh proposals, then hits append-once at ProposeAsync)
        // would orphan them in the forge queue, accumulating without bound.
        var localForgeIds = new List<string>();
        try
        {
            // A re-pull of something already decided at this gate is not re-opened — append-once
            // history stands. Skip WITHOUT parking, so nothing accumulates across repeated pulls.
            if (await store.GetAsync(pkg!.Record.Proposal.Id) is not null)
            {
                return new(PackageAdmission.AlreadyImported,
                    $"already decided at this gate under id '{pkg.Record.Proposal.Id}'", pkg, pkg.Record.Proposal.Id, []);
            }

            // Park the files as LOCAL forge proposals — nothing touches the tree until THIS gate
            // admits. Propose → hold → apply holds for remote code exactly as for a local cycle.
            foreach (var pf in pkg.Files)
            {
                var parked = forge.Add(new ChangeProposal
                {
                    Id = "pkg-" + Guid.NewGuid().ToString("N")[..12],
                    TargetPath = pf.Path.Replace('\\', '/'),
                    NewContent = pf.Content,
                    Summary = $"imported: {pkg.Record.Proposal.Summary}",
                    Reason = $"package sealed by {Fingerprint(pkg.SealSigner)}",
                    CreatedAt = DateTimeOffset.UtcNow,
                });
                localForgeIds.Add(parked.Id);
            }

            var proposal = pkg.Record.Proposal with { ForgeProposalIds = localForgeIds };
            var record = await store.ProposeAsync(policy!, proposal, DateTimeOffset.UtcNow);

            switch (record.State)
            {
                case ProposalState.Admitted:
                    // The apply can still fail closed (a governance path, a locked target). The
                    // admission is durably recorded either way; surface a partial apply as a
                    // WARNING that no green "admitted" line may swallow.
                    try
                    {
                        var applied = ForgeApplier.ApplyAll(forge, localForgeIds, projectDir, "gate",
                            policy!.Sandbox.EnforceWritableAllowlist ? policy.Sandbox.Writable : null);
                        return new(PackageAdmission.Admitted, record.Reason, pkg, record.Proposal.Id, applied);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                    {
                        return new(PackageAdmission.Admitted, record.Reason, pkg, record.Proposal.Id, [],
                            Warning: $"apply did not complete: {ex.Message}");
                    }
                case ProposalState.Held:
                    return new(PackageAdmission.Held, record.Reason, pkg, record.Proposal.Id, []);
                default:
                    ForgeApplier.RejectAll(forge, localForgeIds, "gate", record.Reason);
                    return new(PackageAdmission.Rejected, record.Reason, pkg, record.Proposal.Id, []);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            // Corrupt local key or store, or an unexpected propose failure. Anything parked in
            // this attempt is rejected rather than left orphaned in the queue.
            if (localForgeIds.Count > 0)
            {
                ForgeApplier.RejectAll(forge, localForgeIds, "gate", "import aborted: " + ex.Message);
            }
            return new(PackageAdmission.Refused, ex.Message, pkg, null, []);
        }
    }

    /// <summary>Display fingerprint for a base64 public key, or a plain note when absent.</summary>
    public static string Fingerprint(string? publicKeyBase64) =>
        publicKeyBase64 is null ? "(unsigned)" : OperatorKey.Fingerprint(Convert.FromBase64String(publicKeyBase64));
}
