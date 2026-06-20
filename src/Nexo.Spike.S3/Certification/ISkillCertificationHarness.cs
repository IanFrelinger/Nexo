using Nexo.Spike.S3.Models;

namespace Nexo.Spike.S3.Certification;

public sealed record CertificationResult(
    bool SatisfiesPromotionContract,
    SignedCertificationRecord? Record,
    string? RejectionReason);

public interface ISkillCertificationHarness
{
    Task<CertificationResult> CertifyAsync(SkillCandidate candidate, CancellationToken ct = default);
}
