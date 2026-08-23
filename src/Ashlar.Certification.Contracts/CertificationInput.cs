namespace Ashlar.Certification.Contracts;

/// <summary>
/// Hash-identified truth source or context element consumed while certifying an
/// artifact (trust-loop spec §2.1 <c>inputs</c>).
/// </summary>
public sealed record CertificationInput
{
    /// <summary>Source class of the input (e.g. <c>witness</c>, <c>context</c>, <c>truth-source</c>).</summary>
    public required string Kind { get; init; }

    /// <summary>Identifier of the input within its kind.</summary>
    public required string Id { get; init; }

    /// <summary>Base64 SHA-256 hash of the input's canonical bytes.</summary>
    public required string Hash { get; init; }
}
