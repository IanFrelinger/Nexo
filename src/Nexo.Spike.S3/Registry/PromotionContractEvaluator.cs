using Nexo.Spike.S0.Models;
using Nexo.Spike.S3.Models;

namespace Nexo.Spike.S3.Registry;

public sealed record PromotionContractEvaluation(
    bool Satisfied,
    IReadOnlyList<string> Violations);

/// <summary>
/// Evaluates whether a signed certification record satisfies the S3 promotion contract.
/// </summary>
public static class PromotionContractEvaluator
{
    public static PromotionContractEvaluation Evaluate(SignedCertificationRecord record)
    {
        var violations = new List<string>();

        if (!CertificationRecordSigner.Verify(record))
            violations.Add("certification record signature is invalid");

        if (record.RedReason != RedFailureReason.AssertionFailure)
            violations.Add($"red-reason must be AssertionFailure (was {record.RedReason})");

        if (record.MutationSkipped)
            violations.Add("mutation gate was skipped");
        else if (record.MutationScore < record.MutationThreshold)
            violations.Add($"mutation score {record.MutationScore:F1}% is below threshold {record.MutationThreshold:F1}%");

        if (Math.Abs(record.IntentDensity - 1.0) > 1e-9)
            violations.Add($"intent density must be 1.0 (was {record.IntentDensity:F4})");

        if (record.EscapeRate > 1e-9)
            violations.Add($"escape rate must be 0 (was {record.EscapeRate:F4})");

        if (!record.RoleSeparationAttested)
            violations.Add("role-separation is not attested");

        return new PromotionContractEvaluation(violations.Count == 0, violations);
    }
}
