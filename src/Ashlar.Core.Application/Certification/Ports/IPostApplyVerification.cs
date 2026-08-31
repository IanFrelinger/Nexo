namespace Ashlar.Core.Application.Certification.Ports;

/// <summary>A file that an extension actually wrote to disk: its repo-relative path and full path.</summary>
public sealed record AppliedFile(string RelativePath, string FullPath);

/// <summary>
/// The result of the post-apply canary. <see cref="Passed"/> = false MUST trigger a rollback of the
/// applied writes: an unattended admission whose change does not survive verification may not be
/// left on disk. <see cref="Detail"/> carries the reason for the run summary and the audit record.
/// </summary>
public sealed record PostApplyVerificationResult(bool Passed, string Detail);

/// <summary>
/// The A4 canary: verifies an extension's writes AFTER they land on disk but BEFORE the admission is
/// committed, so an auto-admitted change that turns out to be bad is reverted instead of left to
/// corrupt the node. This is the safety net the unattended auto-apply posture rests on — A2's
/// <see cref="IExtensionCompileCheck"/> gates a proposal on isolated pre-apply evidence; this
/// re-checks the change as it now exists in the working tree and its verdict decides whether the
/// writes stay or are rolled back.
///
/// <para>Implemented in-process (Roslyn over the applied source) so a deployed node with no .NET SDK
/// can still run it. The port is deliberately narrow so a stronger verifier — a full project build,
/// a test course, a runtime smoke probe — can be substituted where those are available, without the
/// apply path changing.</para>
///
/// <para>Fail-closed: any error verifying is a FAILED verification (→ rollback), never a pass. A
/// verifier that cannot decide must not let an unattended change survive.</para>
/// </summary>
public interface IPostApplyVerification
{
    /// <summary>
    /// Verifies the <paramref name="applied"/> files as they now exist under
    /// <paramref name="repoRoot"/>. Non-code files are ignored; an all-docs change verifies
    /// trivially. Never throws for a verification failure — it returns
    /// <see cref="PostApplyVerificationResult.Passed"/> = false with the diagnostics.
    /// </summary>
    Task<PostApplyVerificationResult> VerifyAsync(
        string repoRoot,
        IReadOnlyList<AppliedFile> applied,
        CancellationToken cancellationToken = default);
}
