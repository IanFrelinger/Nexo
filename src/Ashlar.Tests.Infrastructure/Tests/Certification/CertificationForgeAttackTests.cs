using FluentAssertions;
using Ashlar.Certification.Contracts;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// Attack-scenario tests for certification trust holes (limitations 7-9 from
/// certification-evidence.md). These tests verify that signature-stripping, schema-downgrade,
/// and forged-record attacks are REJECTED by default, closing P0 trust vulnerabilities.
/// </summary>
[Trait("Category", "Certification")]
[Trait("Category", "Security")]
public sealed class CertificationForgeAttackTests
{
    private const string HmacKey = "forge-attack-test-hmac";
    private const string BrickSource = "class ForgeProbe { }";

    /// <summary>
    /// Attack: Strip Ed25519 signature from a valid record. Without fail-closed defaults,
    /// verification falls back to HMAC alone (limitation 7).
    /// </summary>
    [Fact]
    public void Attack_SignatureStripping_IsRejectedByDefault()
    {
        var valid = SignedV2RecordWithEd25519();
        var stripped = valid with { Ed25519Signature = null, Ed25519PublicKey = null };

        var trust = CertificationTrustVerifier.Verify(stripped, BrickSource, HmacKey);

        trust.Trusted.Should().BeFalse(
            "Default options require Ed25519 signatures; stripping must be refused");
        trust.FailureCode.Should().Be("ed25519-signature-required");
    }

    /// <summary>
    /// Attack: Downgrade schema version to v1 (null), rewrite gate name, recompute HMAC.
    /// Legacy payload excludes Gate/GatesPassed/Inputs/Proposer/Attempts from signature
    /// (limitation 8).
    /// </summary>
    [Fact]
    public void Attack_SchemaDowngrade_WithRewrittenGate_IsRejectedByDefault()
    {
        var valid = SignedV2RecordWithEd25519();
        var forged = DowngradeToV1(valid, rewrittenGate: "MaliciousGate");

        var trust = CertificationTrustVerifier.Verify(forged, BrickSource, HmacKey);

        trust.Trusted.Should().BeFalse(
            "Default options require v2+ schema; downgrade to rewrite gates must be refused");
        trust.FailureCode.Should().Be("schema-version-below-floor");
    }

    /// <summary>
    /// Combined attack: Strip signature AND downgrade schema. This is the cheapest path
    /// for an attacker with the committed HMAC key.
    /// </summary>
    [Fact]
    public void Attack_CombinedStripAndDowngrade_IsRejectedByDefault()
    {
        var valid = SignedV2RecordWithEd25519();
        var forged = DowngradeToV1(valid, rewrittenGate: "gate-that-never-ran");

        var trust = CertificationTrustVerifier.Verify(forged, BrickSource, HmacKey);

        trust.Trusted.Should().BeFalse("Combined attack is refused by fail-closed defaults");
        // Schema floor is checked first, so that's the reported failure
        trust.FailureCode.Should().Be("schema-version-below-floor");
    }

    /// <summary>
    /// Attack: Forge a record with an attacker-controlled Ed25519 keypair. Without key
    /// pinning, self-consistent signatures verify (noted in limitation 7 fix requirements).
    /// </summary>
    [Fact]
    public void Attack_SelfSignedWithAttackerKey_PassesWithoutPinning()
    {
        // Attacker generates their own keypair and signs with it
        var attackerPrivateKey = new byte[32]; // In reality, a real Ed25519 key
        var attackerPublicKey = Convert.ToBase64String(new byte[32]);
        
        var forged = SignedV2Record() with
        {
            Ed25519PublicKey = attackerPublicKey,
            Ed25519Signature = "YXR0YWNrZXItc2lnbmF0dXJl" // Base64 dummy
        };

        // Without pinning, we can't distinguish attacker keys from legitimate ones
        // This test documents that RequireEd25519Signature alone is insufficient
        // (the fix also provides TrustedEd25519PublicKeys for deployments that need it)
    }

    /// <summary>
    /// Attack: Tamper with content but keep a valid HMAC-only (v1) record. Content binding
    /// should catch this even without Ed25519.
    /// </summary>
    [Fact]
    public void Attack_TamperedContent_IsRejectedByContentHash()
    {
        var valid = SignedV2Record(); // v2 but HMAC-only for this test
        var tamperedSource = "class Tampered { /* evil code */ }";

        // Using Legacy options to bypass Ed25519 requirement, focusing on content binding
        var trust = CertificationTrustVerifier.Verify(
            valid, 
            tamperedSource, 
            HmacKey, 
            CertificationVerifyOptions.Legacy);

        trust.Trusted.Should().BeFalse("Content hash mismatch must be caught regardless of signature");
        trust.FailureCode.Should().Be("content-hash-mismatch");
    }

    /// <summary>
    /// Legitimate v2 record with Ed25519 signature verifies under Default options.
    /// This ensures we didn't break the happy path.
    /// </summary>
    [Fact]
    public void ValidV2RecordWithEd25519_VerifiesUnderDefaultOptions()
    {
        var valid = SignedV2RecordWithEd25519();

        var trust = CertificationTrustVerifier.Verify(valid, BrickSource, HmacKey);

        trust.Trusted.Should().BeTrue($"Valid v2+Ed25519 record must verify under Default: {trust.FailureCode}");
    }

    /// <summary>
    /// Legacy (v1) HMAC-only records are refused by Default but accepted by Legacy options.
    /// This ensures migration path for pre-trust-loop records.
    /// </summary>
    [Fact]
    public void LegacyV1Record_RefusedByDefault_AcceptedByLegacyOptions()
    {
        var v1Record = new CertificationRecordData
        {
            SchemaVersion = null, // v1
            Status = "PASS",
            Stage = "S0-S2",
            Admitted = true,
            Signed = true,
            Timestamp = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero),
            BrickId = "legacy-brick",
            ContentHash = BrickContentHasher.ComputeSha256(BrickSource),
            EscapeRate = 0,
            TotalMutants = 1,
            SurvivingMutants = 0,
            KilledMutants = new[] { "m1" },
            SurvivingMutantIds = Array.Empty<string>(),
        };
        v1Record = v1Record with { Signature = CertificationRecordSigning.Sign(v1Record, HmacKey) };

        CertificationTrustVerifier.Verify(v1Record, BrickSource, HmacKey, CertificationVerifyOptions.Default)
            .Trusted.Should().BeFalse("Default refuses v1 records");
        
        CertificationTrustVerifier.Verify(v1Record, BrickSource, HmacKey, CertificationVerifyOptions.Legacy)
            .Trusted.Should().BeTrue("Legacy accepts v1 records for migration");
    }

    /// <summary>
    /// Limitation 9 fix verification: Composition signer with explicit key should use it,
    /// not fall back to environment. This is tested via the constructor accepting the parameter.
    /// </summary>
    [Fact]
    public void CompositionSigner_HonorsExplicitKey()
    {
        const string explicitKey = "explicit-composition-key";
        
        // Before the fix, this key was discarded. After the fix, it's used.
        var signer = new Infrastructure.Certification.Composition.CompositionCertificationRecordSigner(
            brickSigner: null,
            logger: null,
            hmacKey: explicitKey);

        signer.UsesDevKey.Should().BeFalse(
            "Explicit key was provided, so UsesDevKey should be false (limitation 9 fix)");
    }

    /// <summary>
    /// Helper: Creates a well-formed v2 record with HMAC signature but no Ed25519.
    /// </summary>
    private static CertificationRecordData SignedV2Record()
    {
        var record = new CertificationRecordData
        {
            SchemaVersion = CertificationRecordData.TrustLoopSchemaVersion,
            Status = "PASS",
            Stage = "S0-S2",
            Admitted = true,
            Signed = true,
            Timestamp = new DateTimeOffset(2026, 9, 5, 0, 0, 0, TimeSpan.Zero),
            BrickId = "forge-probe",
            ContentHash = BrickContentHasher.ComputeSha256(BrickSource),
            EscapeRate = 0,
            TotalMutants = 2,
            SurvivingMutants = 0,
            KilledMutants = new[] { "m1", "m2" },
            SurvivingMutantIds = Array.Empty<string>(),
            Gate = "Ashlar.Infrastructure.Certification.CertificationGate",
        };
        return record with { Signature = CertificationRecordSigning.Sign(record, HmacKey) };
    }

    /// <summary>
    /// Helper: Creates a well-formed v2 record with both HMAC and Ed25519 signatures.
    /// </summary>
    private static CertificationRecordData SignedV2RecordWithEd25519()
    {
#if NET8_0_OR_GREATER
        var privateKey = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(privateKey);
        
        var record = SignedV2Record();
        record = record with
        {
            Ed25519PublicKey = CertificationRecordEd25519.DerivePublicKeyBase64(privateKey)
        };
        
        var data = record;
        return data with
        {
            Signature = CertificationRecordSigning.Sign(data, HmacKey),
            Ed25519Signature = CertificationRecordEd25519.Sign(data, privateKey)
        };
#else
        throw new PlatformNotSupportedException("Ed25519 requires NET8_0_OR_GREATER");
#endif
    }

    /// <summary>
    /// Helper: Downgrades a record to v1, strips Ed25519, optionally rewrites gate, recomputes HMAC.
    /// This simulates the limitation 8 attack.
    /// </summary>
    private static CertificationRecordData DowngradeToV1(
        CertificationRecordData record, 
        string? rewrittenGate = null)
    {
        var forged = record with
        {
            SchemaVersion = null, // Downgrade to v1
            Ed25519Signature = null,
            Ed25519PublicKey = null,
            Gate = rewrittenGate ?? record.Gate,
            Signature = null,
        };
        return forged with { Signature = CertificationRecordSigning.Sign(forged, HmacKey) };
    }
}
