using FluentAssertions;
using Nexo.Spike.S0.Models;
using Nexo.Spike.S3.Models;
using Nexo.Spike.S3.Registry;
using Xunit;

namespace Nexo.Tests.Spike.S3;

public sealed class SignedRecordIntegrityTests
{
    [Fact]
    public void Registry_entry_without_valid_signed_record_is_rejected_on_admission()
    {
        var unsigned = new SignedCertificationRecord(
            "unsigned",
            RedFailureReason.AssertionFailure,
            85,
            80,
            false,
            1.0,
            0,
            true,
            "s1.4-v1",
            "s1.4-v1",
            DateTimeOffset.UtcNow,
            string.Empty);

        var evaluation = PromotionContractEvaluator.Evaluate(unsigned);
        evaluation.Satisfied.Should().BeFalse();
        evaluation.Violations.Should().Contain(v => v.Contains("signature"));
    }

    [Fact]
    public void Promotion_contract_requires_assertion_failure_red_reason()
    {
        var record = BuildSignedRecord(RedFailureReason.PassedAgainstStub);
        PromotionContractEvaluator.Evaluate(record).Satisfied.Should().BeFalse();
    }

    [Fact]
    public void Promotion_contract_requires_intent_density_one_and_zero_escape_rate()
    {
        var lowDensity = BuildSignedRecord(density: 0.9);
        PromotionContractEvaluator.Evaluate(lowDensity).Satisfied.Should().BeFalse();

        var escapes = BuildSignedRecord(escapeRate: 0.1);
        PromotionContractEvaluator.Evaluate(escapes).Satisfied.Should().BeFalse();
    }

    [Fact]
    public void CertificationRecordSigner_round_trip_is_deterministic()
    {
        var record = BuildSignedRecord();
        CertificationRecordSigner.Verify(record).Should().BeTrue();
        CertificationRecordSigner.Sign(record).Should().Be(record.Signature);
    }

    private static SignedCertificationRecord BuildSignedRecord(
        RedFailureReason redReason = RedFailureReason.AssertionFailure,
        double density = 1.0,
        double escapeRate = 0)
    {
        var unsigned = new SignedCertificationRecord(
            "record",
            redReason,
            85,
            80,
            false,
            density,
            escapeRate,
            true,
            "s1.4-v1",
            "s1.4-v1",
            DateTimeOffset.UtcNow,
            string.Empty);

        return CertificationRecordSigner.WithSignature(unsigned);
    }
}
