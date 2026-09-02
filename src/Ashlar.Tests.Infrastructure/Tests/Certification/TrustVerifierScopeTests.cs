using FluentAssertions;
using Ashlar.Certification.Contracts;
using Ashlar.Tests.Infrastructure.Certification.Fixtures;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// What "trusted" means when the consumer-facing verifier says it.
///
/// <para>A certification record binds the brick's SOURCE TEXT, because source is what the gate
/// analyzed, mutated and judged. Nothing in the record covers a compiled assembly. So a consumer
/// holding a genuine record, the genuine source, and a DLL built from something else gets a
/// trusted verdict — every check really does pass; the artifact they will execute was simply
/// never one of them. (The kernel's own path does not have this gap: the hot-swap host verifies
/// the record against the source it is about to compile.)</para>
///
/// <para>Binding the artifact is not something this verifier can do — no record format carries
/// an artifact hash. What it must not do is let the gap pass unsaid. These facts pin both halves
/// of that: every trusted verdict names its scope, and a consumer who asks for artifact
/// assurance is REFUSED rather than handed the narrower answer.</para>
/// </summary>
[Trait("Category", "Certification")]
public sealed class TrustVerifierScopeTests
{
    private const string HmacKey = "scope-tests-key";
    private static readonly string BrickSource = MutationProbeBrickSource.Code;

    [Fact]
    public void A_trusted_verdict_names_the_scope_it_was_earned_in()
    {
        var trust = CertificationTrustVerifier.Verify(SignedRecord(), BrickSource, HmacKey);

        trust.Trusted.Should().BeTrue();
        trust.VerifiedScope.Should().Be(CertificationTrustResult.SourceTextScope,
            "a bare boolean lets the reader supply their own idea of what was checked, and the "
            + "idea most readers supply is wider than what this verifier establishes");
    }

    [Fact]
    public void A_consumer_who_needs_the_artifact_bound_is_refused_not_quietly_narrowed()
    {
        var strict = new CertificationVerifyOptions { RequireAssemblyBinding = true };

        var trust = CertificationTrustVerifier.Verify(SignedRecord(), BrickSource, HmacKey, strict);

        trust.Trusted.Should().BeFalse("the check cannot be performed, so it must not be skipped");
        trust.FailureCode.Should().Be("assembly-binding-unavailable");
        trust.Reason.Should().Contain("Fix:");
        strict.IsStrict.Should().BeTrue();
    }

    [Fact]
    public void The_default_options_still_behave_exactly_as_before()
    {
        // Strictness is opt-in; records already on disk must keep verifying.
        var record = SignedRecord();
        CertificationTrustVerifier.Verify(record, BrickSource, HmacKey, CertificationVerifyOptions.Default)
            .Trusted.Should().BeTrue();
        CertificationTrustVerifier.Verify(record, BrickSource, HmacKey, null)
            .Trusted.Should().BeTrue();
        CertificationVerifyOptions.Default.RequireAssemblyBinding.Should().BeFalse();
    }

    [Fact]
    public void An_untrusted_verdict_claims_no_scope_at_all()
    {
        var trust = CertificationTrustVerifier.Verify(SignedRecord(), "different source", HmacKey);

        trust.Trusted.Should().BeFalse();
        trust.VerifiedScope.Should().BeNull("nothing was attested, so nothing may be named as attested");
    }

    private static CertificationRecordData SignedRecord()
    {
        var record = new CertificationRecordData
        {
            Status = "PASS",
            Stage = "S0-S2",
            Admitted = true,
            Signed = true,
            Timestamp = DateTimeOffset.UnixEpoch,
            BrickId = "mutation-probe-brick",
            ContentHash = BrickContentHasher.ComputeSha256(BrickSource),
            EscapeRate = 0,
            TotalMutants = 7,
            SurvivingMutants = 0,
            Gate = "Ashlar.Infrastructure.Certification.CertificationGate",
            SchemaVersion = CertificationRecordData.TrustLoopSchemaVersion,
        };
        return record with { Signature = CertificationRecordSigning.Sign(record, HmacKey) };
    }
}
