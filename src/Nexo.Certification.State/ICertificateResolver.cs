using Nexo.Certification.Contracts;

namespace Nexo.Certification.State;

/// <summary>
/// Resolves a behavior certification content hash to its portable record and brick source.
/// </summary>
public interface ICertificateResolver
{
    CertificateResolveResult Resolve(string behaviorCertContentHash);
}
