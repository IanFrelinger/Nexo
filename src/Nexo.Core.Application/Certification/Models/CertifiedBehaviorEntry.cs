using Nexo.Certification.Contracts;

namespace Nexo.Core.Application.Certification.Models;

/// <summary>
/// Certified behavior resolved by content hash for attested state log verification.
/// </summary>
public sealed class CertifiedBehaviorEntry
{
    public required string ContentHash { get; init; }
    public required CertificationRecordData Record { get; init; }
    public required string Source { get; init; }
}
