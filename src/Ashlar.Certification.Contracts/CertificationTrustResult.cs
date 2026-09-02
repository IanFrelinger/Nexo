namespace Ashlar.Certification.Contracts;

/// <summary>Outcome of external certification trust verification.</summary>
/// <param name="Trusted">True when the record and brick source pass all checks.</param>
/// <param name="FailureCode">Canonical failure code when <paramref name="Trusted"/> is false.</param>
/// <param name="Reason">Human-readable failure explanation.</param>
/// <remarks>
/// <b>Read <see cref="VerifiedScope"/> before treating <paramref name="Trusted"/> as a verdict on
/// anything you are about to run.</b> A bare boolean invites the reader to supply their own idea
/// of what was checked, and the idea most people supply — "this artifact is certified" — is wider
/// than what this verifier can establish.
/// </remarks>
public sealed record CertificationTrustResult(bool Trusted, string? FailureCode, string? Reason)
{
    /// <summary>
    /// The scope in which the record binds today: the brick's SOURCE TEXT.
    /// </summary>
    /// <remarks>
    /// A certification record carries a hash of the source the gate analyzed, mutated and judged.
    /// It carries nothing about a compiled assembly, so a verifier handed a genuine record and a
    /// genuine source can say the record covers that source — and cannot say a word about the DLL
    /// sitting beside them. Consumers that load a prebuilt assembly must close that gap
    /// themselves: compile the verified source (as the kernel's hot-swap host does, verifying the
    /// record against the text it is about to compile), or refuse by setting
    /// <c>CertificationVerifyOptions.RequireAssemblyBinding</c>.
    /// </remarks>
    public const string SourceTextScope = "source-text";

    /// <summary>
    /// What this result actually attests, on a trusted verdict; null when nothing was attested.
    /// </summary>
    /// <remarks>
    /// Present so that "trusted" can never be read wider than it was earned. Today the only value
    /// is <see cref="SourceTextScope"/>: the record is admitted, signed, its signature verifies,
    /// and its content hash matches the supplied source text. NOT included, and not inferable:
    /// any assembly, package or other build output the consumer might execute.
    /// </remarks>
    public string? VerifiedScope { get; init; }
}
