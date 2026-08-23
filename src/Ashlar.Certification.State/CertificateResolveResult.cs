using Ashlar.Certification.Contracts;

namespace Ashlar.Certification.State;

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
