namespace Ashlar.Certification.Contracts;

/// <summary>
/// Portable certification record for external consumers (JSON sidecar).
/// </summary>
public sealed record CertificationRecordData
{
    /// <summary>
    /// Schema version stamped on records minted under the trust-loop spec. Records
    /// with this version sign the extended payload (including <see cref="Gate"/> and
    /// the trust-loop evidence fields); records with a null <see cref="SchemaVersion"/>
    /// are legacy v1 and keep the original payload so existing sidecars stay valid.
    /// </summary>
    public const int TrustLoopSchemaVersion = 2;

    /// <summary>
    /// Schema version stamped on records minted since the gate replays candidate and mutant
    /// code in a bounded child process. Adds <see cref="TimedOutMutants"/> and
    /// <see cref="CrashedMutants"/> to the signed payload, so a mutant the wall clock or a
    /// process death stopped is recorded as exactly that and never as a witness kill. Records at
    /// <see cref="TrustLoopSchemaVersion"/> keep signing the v2 payload and stay valid.
    /// </summary>
    public const int BoundedExecutionSchemaVersion = 3;

    /// <summary>The schema version the gate stamps on every record it mints today.</summary>
    public const int CurrentSchemaVersion = BoundedExecutionSchemaVersion;

    /// <summary>Overall certification outcome (e.g. <c>PASS</c>, <c>FAIL</c>).</summary>
    public required string Status { get; init; }

    /// <summary>Certification pipeline stage that produced this record.</summary>
    public required string Stage { get; init; }

    /// <summary>Whether the brick was admitted into the certified catalog.</summary>
    public required bool Admitted { get; init; }

    /// <summary>Whether an HMAC signature is present and was verified at write time.</summary>
    public required bool Signed { get; init; }

    /// <summary>UTC timestamp when the certification decision was recorded.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Certified brick identifier.</summary>
    public required string BrickId { get; init; }

    /// <summary>SHA-256 hash of canonical brick source bound into this record.</summary>
    public string? ContentHash { get; init; }

    /// <summary>Mutant escape rate observed during certification (0.0–1.0).</summary>
    public double? EscapeRate { get; init; }

    /// <summary>Total mutants generated during certification.</summary>
    public int? TotalMutants { get; init; }

    /// <summary>Mutants that survived the certification test suite.</summary>
    public int? SurvivingMutants { get; init; }

    /// <summary>Identifiers of mutants killed by the test suite.</summary>
    public IReadOnlyList<string> KilledMutants { get; init; } = Array.Empty<string>();

    /// <summary>Identifiers of mutants that survived certification.</summary>
    public IReadOnlyList<string> SurvivingMutantIds { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Identifiers of mutants the wall clock stopped before they finished a witness case. Dead,
    /// but not caught by the witness — so not in <see cref="KilledMutants"/>. Covered by the
    /// signature from <see cref="BoundedExecutionSchemaVersion"/> on.
    /// </summary>
    public IReadOnlyList<string> TimedOutMutants { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Identifiers of mutants whose execution killed the process running them. Dead, but not
    /// caught by the witness — so not in <see cref="KilledMutants"/>. Covered by the signature
    /// from <see cref="BoundedExecutionSchemaVersion"/> on.
    /// </summary>
    public IReadOnlyList<string> CrashedMutants { get; init; } = Array.Empty<string>();

    /// <summary>Base64 HMAC-SHA256 signature over the canonical payload.</summary>
    public string? Signature { get; init; }

    /// <summary>Human-readable reason when certification failed or was deferred.</summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Gate name that emitted or validated this record. Unsigned in legacy (v1)
    /// records; covered by the signature from <see cref="TrustLoopSchemaVersion"/> on.
    /// </summary>
    public string? Gate { get; init; }

    /// <summary>Certificate schema version; null means legacy v1 (pre-trust-loop payload).</summary>
    public int? SchemaVersion { get; init; }

    /// <summary>
    /// Ordered chain of gates evaluated at the recorded content hash (spec §2.1
    /// <c>gates_passed</c>). On FAIL records this holds the gates passed before the
    /// failing gate (R2.4 "furthest gate reached"). The <c>mutation-gate</c> entry's
    /// measured payload remains the record's escape-rate and mutant fields.
    /// </summary>
    public IReadOnlyList<CertificationGatePass> GatesPassed { get; init; } = Array.Empty<CertificationGatePass>();

    /// <summary>Hashes of the truth sources and context used during certification (spec §2.1 <c>inputs</c>).</summary>
    public IReadOnlyList<CertificationInput> Inputs { get; init; } = Array.Empty<CertificationInput>();

    /// <summary>Identity and parameters of the proposer of the certified artifact (spec §2.1 <c>proposer</c>).</summary>
    public CertificationProposer? Proposer { get; init; }

    /// <summary>Effort ledger of proposal/repair attempts (spec §2.1 <c>attempts</c>).</summary>
    public IReadOnlyList<CertificationAttempt> Attempts { get; init; } = Array.Empty<CertificationAttempt>();

    /// <summary>
    /// Base64 Ed25519 signature over the same canonical payload as
    /// <see cref="Signature"/>, written alongside it during the dual-write
    /// transition window. Verified whenever present; absent on HMAC-only records.
    /// </summary>
    public string? Ed25519Signature { get; init; }

    /// <summary>
    /// Base64 raw 32-byte Ed25519 public key of the minter. Part of the signed
    /// payload, so key substitution invalidates the HMAC signature.
    /// </summary>
    public string? Ed25519PublicKey { get; init; }
}
