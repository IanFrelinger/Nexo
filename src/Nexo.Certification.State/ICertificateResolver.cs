using Nexo.Certification.Contracts;

namespace Nexo.Certification.State;

/// <summary>
/// Resolves a behavior certification content hash to its portable record and brick source.
/// </summary>
public interface ICertificateResolver
{
    CertificateResolveResult Resolve(string behaviorCertContentHash);
}

/// <summary>
/// Result of resolving a behavior certification by content hash.
/// </summary>
public sealed class CertificateResolveResult
{
    public CertificateResolveResult(bool found, CertificationRecordData? record, string? brickSource)
    {
        Found = found;
        Record = record;
        BrickSource = brickSource;
    }

    public bool Found { get; }
    public CertificationRecordData? Record { get; }
    public string? BrickSource { get; }
}
