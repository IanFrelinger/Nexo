using Nexo.Certification.Physical;
using Nexo.Certification.Physical.Resolution;

namespace Nexo.Certification.Physical.Issuing;

/// <summary>Result of asset bundle certification.</summary>
/// <param name="Succeeded">Whether certification succeeded.</param>
/// <param name="Bundle">Certified asset bundle when certification succeeds.</param>
/// <param name="FailureCode">Machine-readable failure code when certification is refused.</param>
/// <param name="Reason">Human-readable failure reason when certification is refused.</param>
public sealed record AssetBundleCertificationResult(
    bool Succeeded,
    PhysicalAtomCertBundle? Bundle,
    string? FailureCode,
    string? Reason)
{
    /// <summary>Creates a successful certification result.</summary>
    public static AssetBundleCertificationResult Ok(PhysicalAtomCertBundle bundle) =>
        new(true, bundle, null, null);

    /// <summary>Creates a refused result with a failure code and reason.</summary>
    public static AssetBundleCertificationResult Refused(string code, string reason) =>
        new(false, null, code, reason);
}
