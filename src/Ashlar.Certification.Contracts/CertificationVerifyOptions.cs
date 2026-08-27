namespace Ashlar.Certification.Contracts;

/// <summary>
/// Strictness a verifier applies to a certification record, per SPEC-006 rules S-1 and S-5.
/// </summary>
/// <remarks>
/// <para><b>Every default reproduces today's behaviour exactly.</b> A verifier that passes
/// nothing, or passes <see cref="Default"/>, behaves as it did before this type existed —
/// which is what SPEC-006 S-2 requires, and what keeps records already on disk verifiable.
/// Strictness is opt-in, and turning it on is the remediation, not the declaration.</para>
///
/// <para>This type deliberately declares no cryptography, so it compiles on netstandard2.0
/// where NSec is unavailable. What a netstandard2.0 consumer cannot do is <em>evaluate</em>
/// Ed25519 strictness — see <see cref="RequireEd25519Signature"/>.</para>
/// </remarks>
public sealed class CertificationVerifyOptions
{
    /// <summary>Today's semantics: no floor, no required signature, no pinning.</summary>
    public static CertificationVerifyOptions Default { get; } = new();

    /// <summary>
    /// Lowest <see cref="CertificationRecordData.SchemaVersion"/> this verifier accepts. A
    /// record below the floor is refused outright, before any signature is examined; a null
    /// schema version counts as 0.
    /// </summary>
    /// <remarks>
    /// <b>This is the control that closes the downgrade.</b> `BuildPayload` selects its
    /// canonical form on the record's own <c>SchemaVersion</c>, and the legacy form drops
    /// <c>Gate</c>, <c>GatesPassed</c>, <c>Inputs</c>, <c>Proposer</c>, <c>Attempts</c> and
    /// <c>Ed25519PublicKey</c> out of the signed bytes — so an attacker who strips the
    /// Ed25519 signature and downgrades the schema can rewrite which gates passed under a
    /// recomputed HMAC. Hardening a newer schema version achieves nothing on its own,
    /// because nothing forces a record to use it. Only a floor does.
    /// Set to <see cref="CertificationRecordData.TrustLoopSchemaVersion"/> or higher to
    /// refuse the legacy lane. Default 0 accepts everything, as before.
    /// </remarks>
    public int MinimumSchemaVersion { get; init; }

    /// <summary>
    /// When true, a record without a valid Ed25519 signature is refused rather than falling
    /// back to HMAC alone.
    /// </summary>
    /// <remarks>
    /// Without this, the Ed25519 check is conditional on a field the record itself carries,
    /// so an attacker removes the field rather than forging it. On netstandard2.0 the
    /// signature cannot be evaluated at all, so this option causes a refusal there rather
    /// than a silent pass — never a "verified" verdict a consumer could not actually check.
    /// </remarks>
    public bool RequireEd25519Signature { get; init; }

    /// <summary>
    /// Base64 raw Ed25519 public keys this verifier will accept as signers. Empty or null
    /// disables pinning.
    /// </summary>
    /// <remarks>
    /// <b>Requiring a signature without pinning is close to worthless.</b>
    /// <see cref="CertificationRecordEd25519.VerifySignature"/> verifies against the public
    /// key carried by the record, so a record signed with an attacker's own keypair is
    /// self-consistent and verifies. Pinning is what makes "signed" mean "signed by someone
    /// we accept". Setting this implies <see cref="RequireEd25519Signature"/>: an unsigned
    /// record cannot be pinned.
    /// </remarks>
    public IReadOnlyCollection<string>? TrustedEd25519PublicKeys { get; init; }

    /// <summary>True when any strictness beyond today's behaviour is configured.</summary>
    public bool IsStrict =>
        MinimumSchemaVersion > 0 || RequireEd25519Signature || PinningEnabled;

    /// <summary>True when a non-empty trusted-key set is configured.</summary>
    public bool PinningEnabled => TrustedEd25519PublicKeys is { Count: > 0 };
}
