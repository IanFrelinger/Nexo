namespace Ashlar.Certification.Contracts;

/// <summary>
/// Strictness a verifier applies to a certification record, per SPEC-006 rules S-1 and S-5.
/// </summary>
/// <remarks>
/// <para><b>Default is now fail-closed.</b> Records must use trust-loop schema (v2+) and
/// carry valid Ed25519 signatures to close limitations 7-9 from certification-evidence.md.
/// Use <see cref="Legacy"/> only for testing or migrating pre-trust-loop records.</para>
///
/// <para>This type deliberately declares no cryptography, so it compiles on netstandard2.0
/// where NSec is unavailable. For the same reason it must not <c>cref</c> anything inside
/// <c>CertificationRecordEd25519</c>, whose whole file is fenced behind
/// <c>#if NET8_0_OR_GREATER</c> — an unresolvable cref is CS1574, and this project builds
/// with <c>TreatWarningsAsErrors</c>. What a netstandard2.0 consumer cannot do is <em>evaluate</em>
/// Ed25519 strictness — see <see cref="RequireEd25519Signature"/>.</para>
/// </remarks>
public sealed class CertificationVerifyOptions
{
    /// <summary>
    /// Fail-closed default: trust-loop schema required (v2+), Ed25519 signature required.
    /// This closes signature-stripping (limitation 7) and schema-downgrade (limitation 8)
    /// attacks documented in certification-evidence.md. Use <see cref="Legacy"/> for
    /// pre-trust-loop records or migration paths only.
    /// </summary>
    public static CertificationVerifyOptions Default { get; } = new()
    {
        MinimumSchemaVersion = CertificationRecordData.TrustLoopSchemaVersion,
        RequireEd25519Signature = true,
    };

    /// <summary>
    /// Pre-trust-loop semantics: no floor, no required signature, no pinning. INSECURE.
    /// Only use for testing legacy behavior or during migration from HMAC-only records.
    /// Production deployments should use <see cref="Default"/> or stricter.
    /// </summary>
    public static CertificationVerifyOptions Legacy { get; } = new();

    /// <summary>
    /// Consumer completeness floor: refuse legacy schema and records that do not name a
    /// gate-emitted artifact and a certifier identity. Does not require Ed25519 (that is a
    /// separate trust-root choice) so HMAC-era records that already close class A still verify.
    /// </summary>
    public static CertificationVerifyOptions Strict { get; } = new()
    {
        MinimumSchemaVersion = CertificationRecordData.TrustLoopSchemaVersion,
        RequireGateEmittedArtifact = true,
        RequireCertifierIdentity = true,
    };

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
    /// <c>CertificationRecordEd25519.VerifySignature</c> verifies against the public
    /// key carried by the record, so a record signed with an attacker's own keypair is
    /// self-consistent and verifies. Pinning is what makes "signed" mean "signed by someone
    /// we accept". Setting this implies <see cref="RequireEd25519Signature"/>: an unsigned
    /// record cannot be pinned.
    /// </remarks>
    public IReadOnlyCollection<string>? TrustedEd25519PublicKeys { get; init; }

    /// <summary>
    /// When true, a record without a <c>gate-emitted-artifact</c> input is refused. That input
    /// is the hash of the assembly the certifier compiled and shipped; without it a consumer
    /// cannot tell judged bytes from some other compile of the same source.
    /// </summary>
    public bool RequireGateEmittedArtifact { get; init; }

    /// <summary>
    /// When true, a record without a <c>certifier-identity</c> input is refused. The identity
    /// names the judge; a certificate that omits it cannot be attributed to a gate.
    /// </summary>
    public bool RequireCertifierIdentity { get; init; }

    /// <summary>True when any strictness beyond today's behaviour is configured.</summary>
    public bool IsStrict =>
        MinimumSchemaVersion > 0
        || RequireEd25519Signature
        || PinningEnabled
        || RequireGateEmittedArtifact
        || RequireCertifierIdentity;

    /// <summary>True when a non-empty trusted-key set is configured.</summary>
    public bool PinningEnabled => TrustedEd25519PublicKeys is { Count: > 0 };
}
