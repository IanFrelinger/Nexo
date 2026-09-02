namespace Ashlar.Certification.Contracts;

/// <summary>
/// External consumer verifier: signature + content binding. No gate or generator required.
/// </summary>
/// <remarks>
/// <b>Scope, stated up front.</b> What this verifies is a certification record against a brick's
/// SOURCE TEXT — the record is an admitted PASS, its signature verifies, and its content hash is
/// the hash of the source you handed in. It says nothing whatever about a compiled assembly,
/// because no certification record binds one: the gate analyzes, mutates and judges source. A
/// consumer who verifies a genuine record against genuine source and then loads a DLL that came
/// from somewhere else gets a trusted verdict over an artifact that was never checked. Run the
/// source this record covers (compile it, as <c>CertifiedBrickHotSwapHost</c> does), or set
/// <c>CertificationVerifyOptions.RequireAssemblyBinding</c> to make the missing check a refusal
/// instead of a silence. Every trusted result names its scope in
/// <see cref="CertificationTrustResult.VerifiedScope"/>.
/// </remarks>
public static class CertificationTrustVerifier
{
    /// <summary>
    /// Verifies that a certification record is trusted for the supplied brick source.
    /// Checks admission status, signature validity, and content hash binding.
    /// </summary>
    /// <param name="record">Certification record to verify.</param>
    /// <param name="brickSource">Canonical brick source text to hash and compare.</param>
    /// <param name="hmacKey">Optional HMAC key override for signature verification.</param>
    /// <param name="options">
    /// Strictness to apply (SPEC-006 S-1 and S-5). Null uses
    /// <see cref="CertificationVerifyOptions.Default"/>, which reproduces the behaviour this
    /// method had before the parameter existed.
    /// </param>
    public static CertificationTrustResult Verify(
        CertificationRecordData record,
        string brickSource,
        string? hmacKey = null,
        CertificationVerifyOptions? options = null)
    {
        var strictness = options ?? CertificationVerifyOptions.Default;

        if (!record.Admitted || !string.Equals(record.Status, "PASS", StringComparison.Ordinal))
            return Untrusted("record-not-admitted", "Certification record is not an admitted PASS.");

        // The floor is checked BEFORE any signature, because the schema version selects which
        // canonical payload the signature covers. A downgraded record can carry a perfectly
        // valid HMAC over a payload that omits Gate, GatesPassed, Inputs, Proposer, Attempts
        // and Ed25519PublicKey, so verifying the signature first would answer the wrong
        // question. A null schema version is the legacy lane and counts as 0.
        if ((record.SchemaVersion ?? 0) < strictness.MinimumSchemaVersion)
        {
            return Untrusted(
                "schema-version-below-floor",
                $"Certification record schema version {record.SchemaVersion?.ToString() ?? "(none)"} is below "
                + $"the minimum accepted version {strictness.MinimumSchemaVersion}.");
        }

        if (!record.Signed)
            return Untrusted("record-unsigned", "Certification record is not signed.");

        if (string.IsNullOrWhiteSpace(record.ContentHash))
            return Untrusted("content-hash-missing", "Certification record has no content hash.");

        if (!CertificationRecordSigning.VerifySignature(record, hmacKey))
            return Untrusted("signature-invalid", "Certification record signature is invalid.");

#if NET8_0_OR_GREATER
        // Dual-write window: the Ed25519 signature is enforced whenever present. Presence is
        // controlled by the record's own bytes, so this alone is not a strictness control —
        // an attacker removes the field rather than forging it. RequireEd25519Signature is
        // what turns absence into a refusal.
        if (!string.IsNullOrWhiteSpace(record.Ed25519Signature))
        {
            if (string.IsNullOrWhiteSpace(record.Ed25519PublicKey))
                return Untrusted("ed25519-key-missing", "Certification record carries an Ed25519 signature but no public key.");

            if (!CertificationRecordEd25519.VerifySignature(record))
                return Untrusted("ed25519-signature-invalid", "Certification record Ed25519 signature is invalid.");
        }
        else if (strictness.RequireEd25519Signature || strictness.PinningEnabled)
        {
            return Untrusted(
                "ed25519-signature-required",
                "Certification record carries no Ed25519 signature and this verifier requires one.");
        }

        // Pinning closes the remaining gap: VerifySignature checks the signature against the
        // public key the RECORD carries, so a record signed with an attacker's own keypair is
        // self-consistent and passes. Requiring a signature without pinning only forces the
        // attacker to sign instead of strip.
        if (strictness.PinningEnabled
            && !strictness.TrustedEd25519PublicKeys!.Contains(record.Ed25519PublicKey!, StringComparer.Ordinal))
        {
            return Untrusted(
                "ed25519-key-not-trusted",
                "Certification record is signed by a key this verifier does not accept.");
        }
#else
        // netstandard2.0 has no NSec target, so the signature cannot be evaluated here.
        // Under default options this lane behaves as before (HMAC only, which covers the
        // Ed25519 public key). Under strictness it REFUSES rather than skipping: returning
        // "trusted" for a check that did not run — or stamping a record as pinned when the
        // signature math never executed — would be worse than the silent skip it replaces.
        if (strictness.RequireEd25519Signature || strictness.PinningEnabled)
        {
            return Untrusted(
                "ed25519-verification-unavailable",
                "Ed25519 strictness was requested but this build cannot verify Ed25519 signatures "
                + "(netstandard2.0 has no NSec target). Refusing rather than reporting an unchecked pass.");
        }
#endif

        // What this verifier binds is the brick's SOURCE TEXT. It has no view of any compiled
        // artifact, because no certification record carries one — the gate judges source. A
        // consumer who loads a prebuilt assembly beside a genuine record therefore gets a pass
        // over something the pass never covered. The check cannot be performed here, so under
        // strictness it is REFUSED rather than skipped: a verifier asked for an assurance it
        // cannot produce must say so, never quietly answer the narrower question. (Consumers that
        // compile the verified source instead — as the kernel's hot-swap host does — close the gap
        // properly and should leave this off.)
        if (strictness.RequireAssemblyBinding)
        {
            return Untrusted(
                "assembly-binding-unavailable",
                "This verifier was asked to attest the executable artifact, and cannot: a certification "
                + "record binds the brick's source text and carries nothing about a compiled assembly. "
                + "Fix: compile the source this record covers and run that, instead of a prebuilt "
                + "assembly you cannot bind. Refusing rather than reporting a pass over an artifact that "
                + "was never checked.");
        }

        var actualHash = BrickContentHasher.ComputeSha256(brickSource);
        if (!string.Equals(actualHash, record.ContentHash, StringComparison.Ordinal))
        {
            return Untrusted(
                "content-hash-mismatch",
                $"Brick source hash does not match certified content (expected {record.ContentHash}, got {actualHash}).");
        }

        // Trusted, and explicit about in WHAT: the source text, not the artifact.
        return new CertificationTrustResult(true, null, null)
        {
            VerifiedScope = CertificationTrustResult.SourceTextScope,
        };
    }

    private static CertificationTrustResult Untrusted(string code, string reason) =>
        new(false, code, reason);
}
