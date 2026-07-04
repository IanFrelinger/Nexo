using Nexo.Certification.Physical;

namespace Nexo.Infrastructure.Certification.Physical;

/// <summary>Result of physical-atom bundle certification.</summary>
/// <param name="Succeeded">Whether certification succeeded.</param>
/// <param name="Certificate">Issued physical-atom certificate when certification succeeds.</param>
/// <param name="FailureCode">Machine-readable failure code when certification is refused.</param>
/// <param name="Reason">Human-readable failure reason when certification is refused.</param>
public sealed record BundleCertificationResult(
    bool Succeeded,
    PhysicalAtomCertificate? Certificate,
    string? FailureCode,
    string? Reason)
{
    /// <summary>Creates a successful certification result.</summary>
    public static BundleCertificationResult Ok(PhysicalAtomCertificate certificate) =>
        new(true, certificate, null, null);

    /// <summary>Creates a refused result with a failure code and reason.</summary>
    public static BundleCertificationResult Refused(string code, string reason) =>
        new(false, null, code, reason);
}
