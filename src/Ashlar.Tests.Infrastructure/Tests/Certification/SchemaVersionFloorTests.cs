using FluentAssertions;
using Ashlar.Certification.Contracts;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// Conformance tests for SPEC-006 S-5 (minimum accepted schema version) and the strictness
/// options that close the signature downgrade — limitations 7 and 8 in
/// <c>docs/certification-evidence.md</c>.
///
/// <para>The attack these pin: <c>BuildPayload</c> selects its canonical form on the record's
/// own <c>SchemaVersion</c>, and the legacy form drops <c>Gate</c>, <c>GatesPassed</c>,
/// <c>Inputs</c>, <c>Proposer</c>, <c>Attempts</c> and <c>Ed25519PublicKey</c> out of the
/// signed bytes. So an attacker who strips the Ed25519 signature and nulls the schema version
/// can rewrite <b>which gates passed</b> and recompute the HMAC — and because the default HMAC
/// key is a committed public constant, anyone with the source can. The floor is the only
/// control that refuses this, which is why hardening a newer schema version achieves nothing
/// on its own: nothing forces a record to use it.</para>
/// </summary>
[Trait("Category", "Certification")]
public sealed class SchemaVersionFloorTests
{
    private const string HmacKey = "schema-floor-test-hmac";
    private const string BrickSource = "class FloorProbe { }";

    private static readonly CertificationVerifyOptions Floor =
        new() { MinimumSchemaVersion = CertificationRecordData.TrustLoopSchemaVersion };

    [Fact]
    public void DowngradedRecord_WithRewrittenGate_VerifiesWhenNoFloorIsSet()
    {
        // The hole, demonstrated rather than asserted about. Not a regression test —
        // a record of today's behaviour, which the floor below refuses.
        var forged = Downgrade(SignedV2Record(), rewrittenGate: "gate-that-never-ran");

        var trust = CertificationTrustVerifier.Verify(forged, BrickSource, HmacKey);

        trust.Trusted.Should().BeTrue(
            "with no floor, a legacy-lane record whose HMAC was recomputed verifies — and the "
            + "rewritten gate name is outside the signed bytes entirely");
        forged.Gate.Should().Be("gate-that-never-ran");
    }

    [Fact]
    public void DowngradedRecord_IsRefused_UnderTheFloor()
    {
        var forged = Downgrade(SignedV2Record(), rewrittenGate: "gate-that-never-ran");

        var trust = CertificationTrustVerifier.Verify(forged, BrickSource, HmacKey, Floor);

        trust.Trusted.Should().BeFalse();
        trust.FailureCode.Should().Be("schema-version-below-floor");
    }

    [Fact]
    public void LegitimateV2Record_StillVerifies_UnderTheFloor()
    {
        var trust = CertificationTrustVerifier.Verify(SignedV2Record(), BrickSource, HmacKey, Floor);

        trust.Trusted.Should().BeTrue(
            $"the floor must refuse downgrades without refusing current records ({trust.FailureCode}: {trust.Reason})");
    }

    [Fact]
    public void DefaultOptions_ReproduceTodaysBehaviour()
    {
        var record = SignedV2Record();

        CertificationTrustVerifier.Verify(record, BrickSource, HmacKey, CertificationVerifyOptions.Default)
            .Trusted.Should().BeTrue();
        CertificationTrustVerifier.Verify(record, BrickSource, HmacKey, null)
            .Trusted.Should().BeTrue("null options must behave exactly as the parameterless call did");
    }

    [Fact]
    public void RecordWithoutEd25519_IsRefused_WhenASignatureIsRequired()
    {
        // A record that is HMAC-signed but carries no Ed25519 signature: today's silent
        // fallback, and the thing RequireEd25519Signature turns into a refusal.
        var strict = new CertificationVerifyOptions { RequireEd25519Signature = true };

        var trust = CertificationTrustVerifier.Verify(SignedV2Record(), BrickSource, HmacKey, strict);

        trust.Trusted.Should().BeFalse();
        trust.FailureCode.Should().Be("ed25519-signature-required");
    }

    [Fact]
    public void PinningImpliesASignature_SoAnUnsignedRecordIsRefused()
    {
        var pinned = new CertificationVerifyOptions
        {
            TrustedEd25519PublicKeys = new[] { "c29tZS1vdGhlci1rZXk=" },
        };

        var trust = CertificationTrustVerifier.Verify(SignedV2Record(), BrickSource, HmacKey, pinned);

        trust.Trusted.Should().BeFalse(
            "requiring a signature without pinning only forces an attacker to sign rather than "
            + "strip, so pinning must imply the signature it pins");
        trust.FailureCode.Should().Be("ed25519-signature-required");
    }

    [Fact]
    public void IsStrict_IsFalseOnlyForTodaysSemantics()
    {
        CertificationVerifyOptions.Default.IsStrict.Should().BeFalse();
        Floor.IsStrict.Should().BeTrue();
        new CertificationVerifyOptions { RequireEd25519Signature = true }.IsStrict.Should().BeTrue();
        CertificationVerifyOptions.Default.PinningEnabled.Should().BeFalse();
    }

    /// <summary>A well-formed, HMAC-signed v2 record bound to <see cref="BrickSource"/>.</summary>
    private static CertificationRecordData SignedV2Record()
    {
        var record = new CertificationRecordData
        {
            SchemaVersion = CertificationRecordData.TrustLoopSchemaVersion,
            Status = "PASS",
            Stage = "S0-S2",
            Admitted = true,
            Signed = true,
            Timestamp = new DateTimeOffset(2026, 8, 27, 0, 0, 0, TimeSpan.Zero),
            BrickId = "floor-probe",
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
    /// The downgrade: null the schema version so the legacy payload lane is selected, rewrite
    /// a field that lane does not cover, and recompute the HMAC. Any Ed25519 signature must be
    /// stripped — it is checked against the same <c>BuildPayload</c>, so it would fail here.
    /// </summary>
    private static CertificationRecordData Downgrade(CertificationRecordData signed, string rewrittenGate)
    {
        var forged = signed with
        {
            SchemaVersion = null,
            Ed25519Signature = null,
            Ed25519PublicKey = null,
            Gate = rewrittenGate,
            Signature = null,
        };
        return forged with { Signature = CertificationRecordSigning.Sign(forged, HmacKey) };
    }
}
